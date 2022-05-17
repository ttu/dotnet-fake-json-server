# Developer Notes

### Update package version and create a new release

1. Update version and push to master ([example](https://github.com/ttu/dotnet-fake-json-server/commit/655ae88bb8e100a3eaf8157dfefa20a2539f435c)). Edit version from csproj with an editor.
2. Update Tags
```sh
$ git tag 0.x.x
$ git push origin --tags
```
3. Create new releases archives with a release-script
4. Create new release from [GitHub](https://github.com/ttu/dotnet-fake-json-server/tags) and upload files for the release 
6. Create new global tool. Check API key from [Nuget](https://www.nuget.org/account/apikeys)
```sh
$ dotnet pack --configuration Release
$ dotnet nuget push .\FakeServer\bin\release\FakeServer.0.xx.0.nupkg --source https://api.nuget.org/v3/index.json --api-key xxxxx
```

### Install global tool from local sources

```sh
$ dotnet tool uninstall -g FakeServer
$ dotnet pack --configuration Release --output ./
$ dotnet tool install -g FakeServer --add-source ./
```

Check that installation succeeded. Command should print a correct version number
```sh
$ fake-server --version
```