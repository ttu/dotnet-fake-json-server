FROM microsoft/dotnet:1.1-sdk-msbuild

COPY ./FakeServer /app

WORKDIR /app

RUN ["dotnet", "restore"]

RUN ["dotnet", "build"]

EXPOSE 5000/tcp

CMD ["dotnet", "run", "--url", "http://*:5000"]