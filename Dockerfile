FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR /app

COPY ./FakeServer /app

RUN dotnet restore

RUN dotnet publish -c Release -o out

# Build runtime image
# NOTE: mcr.microsoft.com/dotnet/runtime throws error
FROM mcr.microsoft.com/dotnet/sdk:5.0
WORKDIR /app
COPY --from=build-env /app/out .

#ENV ASPNETCORE_ENVIRONMENT Development

ENTRYPOINT ["dotnet", "FakeServer.dll", "--file", "datastore.json", "--urls", "http://*:57602"]