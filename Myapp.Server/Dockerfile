# Build context is the repo root: docker build -f Myapp.Server/Dockerfile -t myapp-server:local .
FROM mcr.microsoft.com/dotnet/sdk:10.0.400-alpine3.23 AS build
WORKDIR /src
COPY .editorconfig global.json nuget.config Directory.Build.props Directory.Build.rsp Directory.Packages.props BannedSymbols.txt ./
COPY Myapp.Server/Myapp.Server.csproj Myapp.Server/packages.lock.json Myapp.Server/
RUN dotnet restore Myapp.Server/Myapp.Server.csproj --locked-mode
COPY Myapp.Server/ Myapp.Server/
RUN dotnet publish Myapp.Server/Myapp.Server.csproj -c Release --no-restore -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0.12-alpine3.23
WORKDIR /app
COPY --from=build /out ./
ENV ASPNETCORE_URLS=http://+:6300
USER app
EXPOSE 6300
ENTRYPOINT ["dotnet", "Myapp.Server.dll"]
