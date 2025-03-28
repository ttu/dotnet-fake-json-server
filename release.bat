@echo off
SETLOCAL ENABLEDELAYEDEXPANSION

rmdir /s /q releases 
md releases

rmdir /s /q .\FakeServer\bin\release 

set winOS=win-x64
set unixOS=osx-x64 linux-x64

for %%N in (%winOS%) do (
	set rid=%%N
	dotnet publish ./FakeServer/FakeServer.csproj -c release -r !rid! /p:PackAsTool=false /p:PublishTrimmed=true
	7z a -tzip ./releases/fakeserver-!rid!.zip ./FakeServer/bin/release/net8.0/!rid!/publish/* -r
)

for %%N in (%unixOS%) do (
	set rid=%%N
	dotnet publish ./FakeServer/FakeServer.csproj -c release -r !rid! /p:PackAsTool=false /p:PublishTrimmed=true
	7z a -ttar -so ./releases/fakeserver-!rid!.tar ./FakeServer/bin/release/net8.0/!rid!/publish/* -r | 7z a -si ./releases/fakeserver-!rid!.tar.gz
)
