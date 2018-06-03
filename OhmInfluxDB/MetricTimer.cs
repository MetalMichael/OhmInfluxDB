using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using InfluxDB.Collector;
using InfluxDB.Collector.Diagnostics;
using NLog;
using OpenHardwareMonitor.Hardware;

namespace OhmInfluxDB
{
    public class MetricTimer
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

#if DEBUG
        private static bool ShowMetricsFirstRun = true;
#else
        private static bool ShowMetricsFirstRun = false;
#endif

        private readonly Computer _computer;
        private readonly Timer _timer;

        public MetricTimer(Computer computer, TimeSpan interval, string address, string database)
        {
            _computer = computer;
            CollectorLog.RegisterErrorHandler((s, e) =>
            {
                var err = s;
                if (e != null)
                    s += " - " + e.Message;
                Logger.Error(err);
            });

            Metrics.Collector = new CollectorConfiguration()
                .Tag.With("host", Environment.MachineName)
                .Batch.AtInterval(interval)
                .WriteTo.InfluxDB(address, database)
                .CreateCollector();

            _timer = new Timer(interval.TotalMilliseconds) { AutoReset = true };
            _timer.Elapsed += ReportMetrics;
        }

        public void Start()
        {
            _computer.Open();
            _timer.Start();
        }

        public void Stop()
        {
            _computer.Close();
            _timer.Stop();
        }

        private void ReportMetrics(object sender, ElapsedEventArgs e)
        {
            // We don't want to transmit metrics across multiple seconds as they
            // are being retrieved so calculate the timestamp of the signaled event
            // only once.
            long epoch = ((DateTimeOffset) e.SignalTime).ToUnixTimeSeconds();
            try
            {
                SendMetrics(epoch);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unable to send metrics");
            }
        }

        private void SendMetrics(long epoch)
        {
            var sensorCount = 0;
            Metrics.Increment("iterations");

            // Every 5 seconds (or superceding interval) we connect to influxDb
            // and poll the hardware. It may be inefficient to open a new connection
            // every 5 seconds, and there are ways to optimize this, but opening a
            // new connection is the easiest way to ensure that previous failures
            // don't affect future results
            var stopwatch = Stopwatch.StartNew();
            foreach (var sensor in ReadSensors(_computer))
            {
                var data = Normalize(sensor);

                Metrics.Write(data.Identifier, new Dictionary<string, object>
                {
                    {data.Name, data.Value},
                });

                sensorCount++;

                if (ShowMetricsFirstRun)
                    Logger.Info($"Sensor: {data.Identifier} - {data.Name} ({data.Value})");
            }

            ShowMetricsFirstRun = false;
            stopwatch.Stop();
            Logger.Info($"Sent {sensorCount} metrics in {stopwatch.Elapsed.TotalMilliseconds}ms");
        }

        private static Sensor Normalize(Sensor sensor)
        {
            // Take the sensor's identifier (eg. /nvidiagpu/0/load/0)
            // and tranform into nvidiagpu.0.load.<name> where <name>
            // is the name of the sensor lowercased with spaces removed.
            // A name like "GPU Core" is turned into "gpucore". Also
            // since some names are like "cpucore#2", turn them into
            // separate metrics by replacing "#" with "."
            var identifier = sensor.Identifier.Replace('/', '.').Substring(1);
            identifier = identifier.Remove(identifier.LastIndexOf('.'));
            var name = sensor.Name.ToLower().Replace(" ", null).Replace('#', '.');
            return new Sensor(identifier, name, sensor.Value);
        }

        private static IEnumerable<Sensor> ReadSensors(IComputer computer)
        {
            foreach (var hardware in computer.Hardware)
            {
                hardware.Update();
                foreach (var sensor in hardware.Sensors)
                {
                    var id = sensor.Identifier.ToString();

                    // Only report a value if the sensor was able to get a value
                    // as 0 is different than "didn't read". For example, are the
                    // fans really spinning at 0 RPM or was the value not read.
                    if (sensor.Value.HasValue)
                    {
                        yield return new Sensor(id, sensor.Name, sensor.Value.Value);
                    }
                    else
                    {
                        Logger.Warn($"{id} did not have a value");
                    }
                }
            }
        }
    }
}
