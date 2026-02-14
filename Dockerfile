# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/RemoteAgent.Proto/ src/RemoteAgent.Proto/
COPY src/RemoteAgent.Service/ src/RemoteAgent.Service/

RUN dotnet restore src/RemoteAgent.Service/RemoteAgent.Service.csproj && \
    dotnet publish src/RemoteAgent.Service/RemoteAgent.Service.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=build /app/publish .
EXPOSE 5243

ENV ASPNETCORE_URLS=http://0.0.0.0:5243
ENTRYPOINT ["dotnet", "RemoteAgent.Service.dll"]
