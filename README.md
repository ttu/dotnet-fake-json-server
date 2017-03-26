.NET Fake JSON Server
--------------------------

[![Build status](https://ci.appveyor.com/api/projects/status/hacg7qupp5oxbct8?svg=true)](https://ci.appveyor.com/project/ttu/dotnet-fake-json-server)

REST API for developers for prototyping. 
 
* No endpoint configuration required
* .NET Core Web API
* Uses [JSON Flat File DataStore](https://github.com/ttu/json-flatfile-datastore)

##### Example JSON Data

```json
{
  "user": [
    {
      "id": 1,
      "name": "Phil",
      "age": 40,
      "city": "NY"
    },
    {
      "id": 2,
      "name": "Larry",
      "age": 37,
      "city": "London"
    }
  ]
}
```

## Status

```sh
$ curl http://localhost:57602/status
```
```json
{"Status": "Ok"}
```

## Routes

For now supports only id as key field and integer as it's value type.

Dyanamic routes are defined by collection and id: `api/{collection}/{id}`

#####  List collections 

`GET api`

```sh
$ curl http://localhost:57602/api
```

```json
[ "users", "movies" ]
```

##### Get users

`GET api/user`

Returns list of items. Amount of items can be defined with `skip` and `take` query parameters. 

By default returns first 10 items. 
```sh
$ curl http://localhost:57602/api/user
```

Example request returns items from 6 to 26

```sh
$ curl http://localhost:57602/api/user?skip=5&take=20
```


##### Get users with query 

`GET api/user?field=value&otherField=value`

```sh
$ curl http://localhost:57602/api/user?age=40
```
```json
[
    {
        "id": 1,
        "name": "Phil",
        "age": 40,
        "city": "NY"
    },
    {
        "id": 4,
        "name": "Thomas",
        "age": 40,
        "city": "London"
    }
]
```

##### Get user with id 

`GET api/user/{id}`

```sh
$ curl http://localhost:57602/api/user/1
```

```json
{
    "id": 1,
    "name": "Phil",
    "age": 40,
    "city": "NY"
}
```

### License

Licensed under the [MIT](LICENSE) License.
