# OhmInfluxDB

Fork of Nick Babcock's OhmGraphite, for use with InfluxDB.

OhmInfluxDB takes the hard work of extracting hardware sensors from [Open Hardware Monitor](http://openhardwaremonitor.org/) and exports the data in a [InfluxDB](https://www.influxdata.com/time-series-platform/influxdb/) compatible format. If you're missing GPU, temperature, or power metrics in Grafana or (or other graphite UI), this tool is for you!

OhmInfluxDB functions as a console app (cross platform) or a Windows service that periodically polls the hardware. My recommendation is that even though OhmInfluxDB can be run via Mono / Docker, many hardware sensors aren't available in those modes.

Don't fret if this repo hasn't been updated recently. I use this every day to create beautiful dashboards. Keep in mind, Open Hardware Monitor supported components will determine what metrics are available. Below are graphs / stats made strictly from OhmGraphite (additional Windows metrics can be exposed, see [Monitoring Windows system metrics with grafana](https://nbsoftsolutions.com/blog/monitoring-windows-system-metrics-with-grafana))

[![GPU Utilization](https://github.com/MetalMichael/OhmInfluxDB/raw/master/assets/gpu-utilization.png)](#gpu-utilization)
[![Temperatures](https://github.com/MetalMichael/OhmInfluxDB/raw/master/assets/temperatures.png)](#temperatures)
[![Power](https://github.com/MetalMichael/OhmInfluxDB/raw/master/assets/power.png)](#power)
[![Text1](https://github.com/MetalMichael/OhmInfluxDB/raw/master/assets/text1.png)](#text1)
[![Text2](https://github.com/MetalMichael/OhmInfluxDB/raw/master/assets/text2.png)](#text2)

## Getting Started (Windows)

- Create a directory that will home base for OhmInfluxDB (I use C:\Apps\OhmInfluxDB).
- Download the [latest zip](https://github.com/MetalMichael/OhmInfluxDB/releases/latest) and extract to our directory
- Update app configuration (located at `OhmInfluxDB.exe.config`). The config below polls our hardware every `5` seconds and sends the results to a InfluxDB server listening on `http://localhost:8086`.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <appSettings>
	<add key="protocol" value="http" />
    <add key="host" value="localhost" />
    <add key="port" value="8086" />
    <add key="interval" value="5" />
  </appSettings>
</configuration>
```

- This config can be updated in the future, but will require a restart of the app for effect.
- The app can be ran interactively by simply executing `OhmInfluxDB.exe`. Executing as administrator will ensure all sensors are found.
- To install the app `.\OhmInfluxDB.exe install`. The command will install OhmInfluxDB as a Windows service (so you can manage it with your favorite powershell commands or `services.msc`)
- To start the app after installation: `.\OhmInfluxDB.exe start` or your favorite Windows service management tool

### Upgrades

- Stop OhmInfluxDB service
- Unzip latest release over previous directory
- Start OhmInfluxDB service

## Getting Started (Docker)

Since the full gambit of metrics aren't available in a Docker container, I've refrained from putting the project on docker hub lest it misleads people to think otherwise.
(Also hasn't been tested on docker since port to InfluxDB.)

```bash
docker build -t MetalMichael/ohm-influxdb .
docker run -v $PWD/app.config:/opt/InfluxDb/OhmInfluxDB.exe.config:ro MetalMichael/ohm-influxdb
```

`app.config` is in the same format as the above configuration.
