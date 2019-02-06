# https://docs.docker.com/engine/examples/dotnetcore/
FROM microsoft/dotnet:sdk AS build-env
WORKDIR /app

COPY ./FakeServer /app

RUN dotnet restore

RUN dotnet publish -c Release -o out

# Build runtime image
FROM microsoft/dotnet:aspnetcore-runtime
WORKDIR /app
COPY --from=build-env /app/out .

#ENV ASPNETCORE_ENVIRONMENT Development

ENTRYPOINT ["dotnet", "FakeServer.dll", "--file", "datastore.json", "--urls", "http://*:57602"]