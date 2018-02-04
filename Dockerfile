FROM mono:5.4.0.201 as builder
COPY . /tmp/
WORKDIR /tmp
RUN msbuild /t:restore /t:build /p:Configuration=Release && \
    rm /tmp/OhmInfluxDb/bin/Release/net46/OhmInfluxDb.exe.config

FROM mono:5.4.0.201
COPY --from=builder /tmp/OhmInfluxDb/bin/Release/net46 /opt/OhmInfluxDb
COPY --from=builder /tmp/OhmInfluxDb/NLog.docker.config /opt/OhmInfluxDb/NLog.config
WORKDIR /opt/OhmInfluxDb
VOLUME /opt/OhmInfluxDb/OhmInfluxDb.exe.config
CMD mono OhmInfluxDb.exe
