# Expects pre-built service output in service-publish/ (from Build solution job artifact).
# Build context must include: this Dockerfile and the service-publish/ directory.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN useradd -r -s /bin/false appuser && chown -R appuser /app
USER appuser

COPY service-publish/ .
EXPOSE 5243

ENV ASPNETCORE_URLS=http://0.0.0.0:5243
ENTRYPOINT ["dotnet", "RemoteAgent.Service.dll"]
