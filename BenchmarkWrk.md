## Benchmark with wrk

[wrk installation guide](https://github.com/wg/wrk/wiki/Installing-Wrk-on-Linux)

Start the server from the command line.

```sh
$ dotnet run --urls http://localhost:57602
```

Do first benchmark against `/api` endpoint using OPTIONS method, as it only uses OptionsMiddleware.

Create a script file (e.g. options.lua) for `OPTIONS` request.
```lua
wrk.method = "OPTIONS"
```

Execute OPTIONS benchmark for 10 seconds.

```sh
$ wrk -c 256 -t 32 -d 10 -s options.lua http://localhost:57602/api
```

Create a script file (e.g. post.lua) for `POST` request.

```lua
wrk.method = "POST"
wrk.body   = "{ \"name\": \"Benchmark User\", \"age\": 50, \"location\": \"NY\" }"
wrk.headers["Content-Type"] = "application/json"
```

Execute POST benchmark for 10 seconds.

```sh
$ wrk -c 256 -t 32 -d 10 -s post.lua http://localhost:57602/api/users
```