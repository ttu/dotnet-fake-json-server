@echo off
rm -rf releases
md releases

rm -rf ./FakeServer/bin/release

dotnet publish ./FakeServer/FakeServer.csproj -c release -r win10-x64
dotnet publish ./FakeServer/FakeServer.csproj -c release -r osx.10.11-x64
dotnet publish ./FakeServer/FakeServer.csproj -c release -r osx.10.12-x64
dotnet publish ./FakeServer/FakeServer.csproj -c release -r ubuntu.16.04-x64
dotnet publish ./FakeServer/FakeServer.csproj -c release -r ubuntu.16.10-x64

7z a -t7z ./releases/win10-x64.7z ./FakeServer/bin/release/netcoreapp1.1/win10-x64/publish/* -r
7z a -t7z ./releases/osx.10.11-x64.7z ./FakeServer/bin/release/netcoreapp1.1/osx.10.11-x64/publish/* -r
7z a -t7z ./releases/osx.10.12-x64.7z ./FakeServer/bin/release/netcoreapp1.1/osx.10.12-x64/publish/* -r
7z a -t7z ./releases/ubuntu.16.04-x64.7z ./FakeServer/bin/release/netcoreapp1.1/ubuntu.16.04-x64/publish/* -r
7z a -t7z ./releases/ubuntu.16.10-x64.7z ./FakeServer/bin/release/netcoreapp1.1/ubuntu.16.10-x64/publish/* -r


 