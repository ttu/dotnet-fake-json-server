#!/bin/bash

rm -rf releases 
mkdir releases

rm -rf ./FakeServer/bin/release 

declare -a winOS=("win10-x64")
declare -a unixOS=("osx.10.11-x64" "osx.10.12-x64" "ubuntu.16.04-x64" "ubuntu.16.10-x64")

for rid in "${winOS[@]}"
do
	dotnet publish ./FakeServer/FakeServer.csproj -c release -r $rid
	cd ./FakeServer/bin/release/netcoreapp2.0/$rid/publish/
	zip -r ../../../../../../releases/fakeserver-$rid.zip .
	cd ../../../../../../
done

for rid in "${unixOS[@]}"
do
	dotnet publish ./FakeServer/FakeServer.csproj -c release -r $rid
	cd ./FakeServer/bin/release/netcoreapp2.0/$rid/publish/
	tar -cvzf ../../../../../../releases/fakeserver-$rid.tar.gz *
	cd ../../../../../../
done