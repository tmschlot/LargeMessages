using Amazon.CloudWatchLogs;
using Amazon.Runtime;
using Serilog;
using Serilog.Sinks.AwsCloudWatch;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LargeMessages
{
    class Program
    {
        static AWSCredentials credentials = null;

        static async Task Main(string[] args)
        {
            switch (args.Length)
            {
                case 1:
                    credentials = new StoredProfileAWSCredentials(args[0]); // profile credentials
                    break;
                case 2:
                    credentials = new BasicAWSCredentials(args[0], args[1]); // given
                    break;
                default:
                    // from environment
                    break;
            }

            ConfigureLogging();

            for (var i = 0; i < 10; i++) // run the simulation 10x
            {
                for (var j = 0; j < 260; j++) // log lots of messages
                {
                    System.Diagnostics.Debug.WriteLine("{0}.{1}", i, j);
                    var message = CreateMessage(i, j);
                    Log.Logger.Information(message);
                }
            }

            Console.WriteLine("waiting for the app to finish...");
            await Task.Delay(TimeSpan.FromMinutes(1));
        }

        static string CreateMessage(int run, int index)
        {
            /*
             * when index = 0, string will be ~1*1024 bytes, or 1KB
             * when index = 259, string will be ~260*1024 bytes, or 260KB (max event size = 256KB; max batch size = 1MB)
             */
            return $"{run}.{index} {string.Concat(Enumerable.Range(1, (index + 1) * 1024).SelectMany(item => "a").ToArray())}";
        }

        static void ConfigureLogging()
        {
            var selflog = new LoggerConfiguration()
                .WriteTo.LiterateConsole()
                .CreateLogger();
            Serilog.Debugging.SelfLog.Enable(msg => selflog.Warning(msg));

            AmazonCloudWatchLogsClient client = null;
            if (credentials != null)
            {
                client = new AmazonCloudWatchLogsClient(credentials, Amazon.RegionEndpoint.USWest2);
            }
            else
            {
                client = new AmazonCloudWatchLogsClient(Amazon.RegionEndpoint.USWest2);
            }

            Log.Logger = new LoggerConfiguration()
                .WriteTo.AmazonCloudWatch(new CloudWatchSinkOptions
                {
                    LogGroupName = "/aaa/test/logging",
                    Period = TimeSpan.FromSeconds(1), // really short only for the purpose of not taking forever
                    // BatchSizeLimit = default = 100
                }, client)
                .CreateLogger();
        }
    }
}
