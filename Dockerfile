FROM microsoft/aspnetcore-build

COPY ./FakeServer /app

WORKDIR /app

RUN dotnet restore

RUN dotnet build

EXPOSE 57602/tcp

CMD ["dotnet", "run", "--file", "datastore.json", "--urls", "http://*:57602"]