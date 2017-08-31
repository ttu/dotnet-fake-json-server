@echo off
SETLOCAL ENABLEDELAYEDEXPANSION

rmdir /s /q releases 
md releases

rmdir /s /q .\FakeServer\bin\release 

set winOS=win10-x64
set unixOS=osx.10.11-x64 osx.10.12-x64 ubuntu.16.04-x64 ubuntu.16.10-x64

for %%N in (%winOS%) do (
	set rid=%%N
	dotnet publish ./FakeServer/FakeServer.csproj -c release -r !rid!
	7z a -tzip ./releases/fakeserver-!rid!.zip ./FakeServer/bin/release/netcoreapp1.1/!rid!/publish/* -r
)

for %%N in (%unixOS%) do (
	set rid=%%N
	dotnet publish ./FakeServer/FakeServer.csproj -c release -r !rid!
	7z a -ttar -so ./releases/fakeserver-!rid!.tar ./FakeServer/bin/release/netcoreapp1.1/!rid!/publish/* -r | 7z a -si ./releases/fakeserver-!rid!.tar.gz
)
