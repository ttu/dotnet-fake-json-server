.NET Fake JSON Server
--------------------------

[![Build Status](https://travis-ci.org/ttu/dotnet-fake-json-server.svg?branch=master)](https://travis-ci.org/ttu/dotnet-fake-json-server) [![Build status](https://ci.appveyor.com/api/projects/status/hacg7qupp5oxbct8?svg=true)](https://ci.appveyor.com/project/ttu/dotnet-fake-json-server)

Fake REST API for prototyping or as a CRUD backend.

* No need to define types for resources. Types are handled dynamically
* No database. Data is stored to a flat JSON file
* CRUD operations (GET, PUT, POST, PATCH, DELETE)
* Start server and API is ready to be used with any data

## Features
 
* .NET Core Web API
* Uses [JSON Flat File DataStore](https://github.com/ttu/json-flatfile-datastore)
  * All changes are automatically saved to defined JSON file
* Token authentication
  * Add allowed usernames/passwords to `authentication.json`
* WebSockets
* Static files
* Swagger
* CORS

## Get started

```sh
$ git clone https://github.com/ttu/dotnet-fake-json-server.git
$ cd dotnet-fake-json-server/FakeServer
$ dotnet run [--filename] [--url]

# Optional arguments:
#   --filename        Datastore's JSON file (default datastore.json)
#   --url             Server url (default http://localhost:57602)      

# Example: Start server
$ dotnet run --filename data.json --url http://localhost:57602

# List collections (should be empty, if data.json didn't exist before)
$ curl http://localhost:57602/api

# Insert new user
$ curl -H "Accept: application/json" -H "Content-type: application/json" -X POST -d '{ "name": "Phil", "age": 20, "location": "NY" }' http://localhost:57602/api/user/

# Insert another user
$ curl -H "Accept: application/json" -H "Content-type: application/json" -X POST -d '{ "name": "James", "age": 40, "location": "SF" }' http://localhost:57602/api/user/

# List users
$ curl http://localhost:57602/api/user

# List users from NY
$ curl http://localhost:57602/api/user?location=NY

# Get User with Id 1
$ curl http://localhost:57602/api/user/1

...

# Add users to data.json manually

# Command DataStore to reload data from the file (normally refreshes only on initialization or on data writes)
$ curl -X POST http://localhost:57602/api/reload/

# Get all users
$ curl http://localhost:57602/api/user/
...

# Or open url http://localhost:57602/swagger/ with browser and use Swagger
```

#### Docker

```sh
$ docker build -t fakeapi .
$ docker run -it -p 5000:5000 fakeapi
```

## Routes

```
GET    /
POST   /token
GET    /status
POST   /admin/reload
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

##### Reload

Reload endpoint, which reloads JSON data from the file to DataStore. DataStore updates internal data from the file only when initialized and when data is updated, so in case that JSON file is updated manually and new data is requested immediately before any updates, this must be called before request. Endoint is in Admin controller, so it is usable also through Swagger.

```sh
$ curl -X POST http://localhost:57602/admin/reload --data ""
```

##### WebSockets

API will send latest update's method (`POST, PUT, PATCH, DELETE`) and path with WebSocket.

```json
{ "method": "PATCH", "path": "/api/user/2" }
```

`index.html` has a WebSocket example.

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
GET api/{item}?field=value&otherField=value

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

`GET api/{item}?parent.child.grandchild.field=value`

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

### Benchmark

Install ApacheBench
```sh
$ sudo apt-get install apache2-utils
```

Do benchmark against status endpoint, as it doesn't use any middlewares and it doesn't do any processing.
```sh
$ ab -c 10 -n 2000 http://localhost:57602/status
```

Create a POST data JSON file (e.g. user.json)
```json
{ "name": "Benchmark User", "age": 50, "location": "NY" }
```

Execute POST 2000 times with 10 concurrent connections
```sh
$ ab -p user.json -T application/json -c 10 -n 2000 http://localhost:57602/api/user
```

### License

Licensed under the [MIT](LICENSE) License.
