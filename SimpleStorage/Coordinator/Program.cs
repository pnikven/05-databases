using System;
using System.Threading;
using CommandLine;
using CommandLine.Text;
using Coordinator.IoC;
using Microsoft.Owin.Hosting;

namespace Coordinator
{
    public class Program
    {
        public static bool ShouldRunInfinitely { get; set; }
        public static void Main(string[] args)
        {
            var options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                var configuration = new Configuration {ShardCount = options.ShardCount};
                IoCFactory.GetContainer().Configure(c => c.For<IConfiguration>().Use(configuration));
                using (WebApp.Start<Startup>(string.Format("http://+:{0}/", options.Port)))
                {
                    Console.WriteLine("Server running on port {0}", options.Port);

                    if (ShouldRunInfinitely)
                        new ManualResetEvent(false).WaitOne();

                    Console.ReadLine();
                }
            }
        }

        private class Options
        {
            [Option('p', Required = true, HelpText = "Port.")]
            public int Port { get; set; }

            [Option('c', Required = true, HelpText = "Shard count.")]
            public int ShardCount { get; set; }

            [HelpOption]
            public string GetUsage()
            {
                var result = HelpText.AutoBuild(this,
                    current => HelpText.DefaultParsingErrorsHandler(this, current));
                return result;
            }
        }
    }
}
