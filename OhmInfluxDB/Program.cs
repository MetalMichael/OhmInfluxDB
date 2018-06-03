using System;
using OpenHardwareMonitor.Hardware;
using System.Configuration;
using NLog;
using Topshelf;

namespace OhmInfluxDB
{
    class Program
    {
        private static string Host = "localhost";
        private static int Port = 8086;
        private static string Protocol = "http";
        private static int Timeout = 5;
        private static string Database = "ohm";

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<MetricTimer>(s =>
                {
                    // We need to know where the influxDb server lives and how often
                    // to poll the hardware
                    ParseConfig();

                    // We'll want to capture all available hardware metrics
                    // to send to influxDb
                    var computer = new Computer
                    {
                        GPUEnabled = true,
                        MainboardEnabled = true,
                        CPUEnabled = true,
                        RAMEnabled = true,
                        FanControllerEnabled = true,
                        HDDEnabled = true
                    };

                    var address = $"{Protocol}://{Host}:{Port}";
                    s.ConstructUsing(name => new MetricTimer(computer, TimeSpan.FromSeconds(Timeout), address, Database));
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("Extract hardware sensor data and export it to a given host and port in an InfluxDB compatible format");
                x.SetDisplayName("Ohm InfluxDB");
                x.SetServiceName("OhmInfluxDB");
                x.OnException(ex => Logger.Error(ex, "OhmInfluxDB TopShelf encountered an error"));
            });
        }

        private static void ParseConfig()
        {
            if (ConfigurationManager.AppSettings["host"] != null)
                Host = ConfigurationManager.AppSettings["host"];

            if (ConfigurationManager.AppSettings["protocol"] != null)
                Protocol = ConfigurationManager.AppSettings["protocol"];

            if (ConfigurationManager.AppSettings["database"] != null)
                Database = ConfigurationManager.AppSettings["database"];

            int.TryParse(ConfigurationManager.AppSettings["port"], out Port);
            int.TryParse(ConfigurationManager.AppSettings["interval"], out Timeout);

            Logger.Info($"Host: {Host} port: {Port} interval: {Timeout}");
        }
    }
}
