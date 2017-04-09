.NET Fake JSON Server
--------------------------

[![Build Status](https://travis-ci.org/ttu/dotnet-fake-json-server.svg?branch=master)](https://travis-ci.org/ttu/dotnet-fake-json-server) [![Build status](https://ci.appveyor.com/api/projects/status/hacg7qupp5oxbct8?svg=true)](https://ci.appveyor.com/project/ttu/dotnet-fake-json-server)

Fake REST API for developers for prototyping
 
* .NET Core Web API
* Uses [JSON Flat File DataStore](https://github.com/ttu/json-flatfile-datastore)
  * All changes are automatically saved to `datastore.json`
* CORS
* Static files
* Swagger
* Token authentication
  * Add allowed usernames/passwords to `authentication.json`
* WebSockets

## Routes

```
GET    /
POST   /token
GET    /status
GET    /api
GET    /api/{item}
POST   /api/{item}
GET    /api/{item}/{id}
PUT    /api/{item}/{id}
PATCH  /api/{item}/{id}
DELETE /api/{item}/{id}
```

Dynamic routes are defined by the name of item's collection and id: `api/{item}/{id}`. All examples below use `user` as a collection name.

For now API supports only id as the key field and integer as it's value type.

#### Swagger

Swagger is configured to endpoint `/swagger` and Swagger UI opens when project is started.

#### Static Files

`GET /`

Returns static files from wwwroot. Default file is `index.html`.

#### CORS

CORS is enabled and it allows everything.

#### Authentication

Fake REST API supports token authentication. API has a token provider middleware which provides an endpoint for token generation `/token`.

Authentiation can be disabled from `authentiation.json` by setting Enabled to `false`.

```json
{
  "Authentication": {
    "Enabled": true,
    "Users": [
        { "Username": "admin", "Password": "root" }
    ]
  }
}
```

Check SimpleTokenProvider from [GitHub](https://github.com/nbarbettini/SimpleTokenProvider) and [StormPath's blog post](https://stormpath.com/blog/token-authentication-asp-net-core).

Get token:
```sh
$ curl -X POST -H 'content-type: multipart/form-data' -F username=admin -F password=root http://localhost:57602/token
```

Add token to Authorization header:
```sh
$ curl -H 'Authorization: Bearer [TOKEN]' http://localhost:57602/api
```

##### Status

Status endpoint, which returns current status of the service.

```sh
$ curl http://localhost:57602/status
```
```json
{"status": "Ok"}
```

##### WebSockets

API will send latest update's method (`POST, PUT, PATCH, DELETE`) and path with WebSocket.

```json
{ "method": "PATCH", "path": "/api/user/2" }
```

`index.html`has a WebSocket example.

##### Example JSON Data

Data used in examples

```json
{
  "user": [
    { "id": 1, "name": "Phil", "age": 40, "location": "NY" },
    { "id": 2, "name": "Larry", "age": 37, "location": "London" },
    { "id": 3, "name": "Thomas", "age": 40, "location": "London" }
  ],
  "movie": []
}
```

#####  List collections 

```
GET /api

200 OK : List of collections
```

```sh
$ curl http://localhost:57602/api
```

```json
[ "user", "movie" ]
```

##### Get items

```
GET /api/{item}

200 OK        : Collection is found
404 Not Found : Collection is not found or it is empty
```

Amount of items can be defined with `skip` and `take` parameters. By default request returns first 10 items.

```sh
$ curl http://localhost:57602/api/user
```

Example request returns items from 6 to 26.

```sh
$ curl http://localhost:57602/api/user?skip=5&take=20
```


##### Get items with query 

```
GET api/user?field=value&otherField=value

200 OK        : Collection is found
404 Not Found : Collection is not found or it is empty
```

```sh
$ curl http://localhost:57602/api/user?age=40
```
```json
[ 
 { "id": 1, "name": "Phil", "age": 40, "location": "NY" },
 { "id": 3, "name": "Thomas", "age": 40, "location": "London" }
]
```

Query can have path to child properties. Property names are separated by periods.

`GET api/user?parent.child.grandchild.field=value`

Example JSON:
```json
[
  {
    "companyName": "ACME",
    "employees": [ 
      { "id": 1, "name": "Thomas", "address": { "city": "London" } }
    ]
  },
  {
    "companyName": "Box Company",
    "employees": [ 
      { "id": 1, "name": "Phil", "address": { "city": "NY" } }
    ]
  }
]
```

Query would return ACME from the example JSON.

```sh
$ curl http://localhost:57602/api/user?employees.address.city=London
```

```json
[
  {
    "companyName": "ACME",
    "employees": [ 
      { "id": 1, "name": "Thomas", "address": { "city": "London" } }
    ]
  }
]
```

##### Get item with id 

``` 
GET /api/{item}/{id}

200 OK        : Item is found
404 Not Found : Item is not found
```

```sh
$ curl http://localhost:57602/api/user/1
```

```json
{ "id": 1, "name": "Phil", "age": 40, "location": "NY" }
```

##### Get nested items

```
GET /api/{item}/{id}/{restOfThePath}

200 OK          : Nested item is found
400 Bad Request : Parent item is not found
404 Not Found   : Nested item is not found
```

It is possible to request only child objects instead of full item. Path to nested item can contain id field integers and property names.

```json
[
  {
    "id": 0,
    "companyName": "ACME",
    "employees": [ 
      { "id": 1, "name": "Thomas", "address": { "city": "London" } }
    ]
  }
]
```

Example query will return address object from the employee.

```sh
$ curl http://localhost:57602/api/company/0/employees/1/address
```

```json
{ "address": { "city": "London" } }
```

##### Add item 

```
POST /api/{item}

200 OK : New item is created
```

```sh
$ curl -H "Accept: application/json" -H "Content-type: application/json" -X POST -d '{ "name": "Phil", "age": 40, "location": "NY" }' http://localhost:57602/api/user/
```
Response has new item's id

```json
{ "id": 6 }
```

##### Replace item 

``` 
PUT /api/{item}/{id}

200 OK        : Item is replaced
404 Not Found : Item is not found
```

```sh
$ curl -H "Accept: application/json" -H "Content-type: application/json" -X PUT -d '{ "name": "Roger", "age": 28, "location": "SF" }' http://localhost:57602/api/user/1
```

##### Update item 

```
PATCH /api/{item}/{id}

200 OK          : Item updated
400 Bad Request : PATCH is empty
404 Not Found   : Item is not found
```

```sh
$ curl -H "Accept: application/json" -H "Content-type: application/json" -X PATCH -d '{ "name": "Timmy" }' http://localhost:57602/api/user/1
```

##### Delete item 

``` 
DELETE /api/{item}/{id}

200 OK        : Item deleted
404 Not Found : Item is not found
```

```sh
$ curl -X DELETE http://localhost:57602/api/user/1
```

### License

Licensed under the [MIT](LICENSE) License.
