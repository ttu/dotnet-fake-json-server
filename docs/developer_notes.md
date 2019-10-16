# Developer Notes


### Install global tool from local sources

```sh
$ dotnet tool uninstall -g FakeServer
$ dotnet pack --configuration Release --output ./
$ dotnet tool install -g FakeServer --add-source ./
```

Check that installation succeeded (should print version number):
```sh
$ fake-server --version
```