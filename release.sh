#!/bin/bash

rm -rf releases 
mkdir releases

rm -rf ./FakeServer/bin/release 

declare -a winOS=("win-x64")
declare -a unixOS=("osx-x64" "linux-x64")

for rid in "${winOS[@]}"
do
	dotnet publish ./FakeServer/FakeServer.csproj -c release -r $rid /p:PackAsTool=false
	cd ./FakeServer/bin/release/netcoreapp2.2/$rid/publish/
	zip -r ../../../../../../releases/fakeserver-$rid.zip .
	cd ../../../../../../
done

for rid in "${unixOS[@]}"
do
	dotnet publish ./FakeServer/FakeServer.csproj -c release -r $rid /p:PackAsTool=false
	cd ./FakeServer/bin/release/netcoreapp2.2/$rid/publish/
	tar -cvzf ../../../../../../releases/fakeserver-$rid.tar.gz *
	cd ../../../../../../
done