.NET Fake JSON Server
--------------------------

[![Build Status](https://travis-ci.org/ttu/dotnet-fake-json-server.svg?branch=master)](https://travis-ci.org/ttu/dotnet-fake-json-server) [![Build status](https://ci.appveyor.com/api/projects/status/hacg7qupp5oxbct8?svg=true)](https://ci.appveyor.com/project/ttu/dotnet-fake-json-server)

Fake REST API for prototyping or as a CRUD backend.

* No need to define types for resources. Types are handled dynamically
* No database. Data is stored to a JSON file
* CRUD operations (GET, PUT, POST, PATCH, DELETE)
* Async versions of update operations with long running jobs
* Simulate delay and errors for requests
* No configuration needed, start the Server and API is ready to be used with any data

## Features
 
* .NET Core Web API
* Can be used without .NET with Docker
* Uses [JSON Flat File DataStore](https://github.com/ttu/json-flatfile-datastore)
  * All changes are automatically saved to defined JSON file
* Token authentication
  * Add allowed usernames/passwords to `authentication.json`
* WebSockets
* Static files
* Swagger
* CORS

## Get started

Get source code from GitHub

```sh
$ git clone https://github.com/ttu/dotnet-fake-json-server.git
```

#### Start with .NET CLI

```sh
$ cd dotnet-fake-json-server/FakeServer
$ dotnet run [--filename] [--server.urls]

# Optional arguments:
#   --filename        Datastore's JSON file (default datastore.json)
#   --server.urls     Server url (default http://localhost:57602)      

# Example: Start server
$ dotnet run --filename data.json --server.urls http://localhost:57602
```

#### Docker

If you don't have .NET installed, you can run server with Docker.

```sh
$ cd dotnet-fake-json-server
$ docker build -t fakeapi .

# Run in foreground
$ docker run -it -p 57602:57602 fakeapi

# Run in detached mode (run in background)
$ docker run -it -d -p 57602:57602 fakeapi
```

Copy JSON-file to container. Filename is `db.json`

```sh
# Check container id (image name is fakeapi)
$ docker ps

# Copy file from host to container
$ docker cp db.json [ContainerId]:/app/db.json

# Copy file from container to host
$ docker cp [ContainerId]:/app/db.json db.json
```

`docker run` will reset JSON-file, so copy it before closing the server.

#### Quick example

```sh
# List collections (should be empty, if data.json didn't exist before)
$ curl http://localhost:57602/api

# Insert new user
$ curl -H "Accept: application/json" -H "Content-type: application/json" -X POST -d '{ "name": "Phil", "age": 20, "location": "NY" }' http://localhost:57602/api/users/

# Insert another user
$ curl -H "Accept: application/json" -H "Content-type: application/json" -X POST -d '{ "name": "James", "age": 40, "location": "SF" }' http://localhost:57602/api/users/

# List users
$ curl http://localhost:57602/api/users

# List users from NY
$ curl http://localhost:57602/api/users?location=NY

# Get User with Id 1
$ curl http://localhost:57602/api/users/1

...

# Add users to data.json manually

# Get all users
$ curl http://localhost:57602/api/users/
...

# Or open url http://localhost:57602/swagger/ with browser and use Swagger
```

#### Redux TodoMVC Example

[Redux TodoMVC](https://github.com/ttu/todomvc-fake-server) example modified to use Fake JSON Server as a Back End.

## Features

### Authentication

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

### WebSockets

API will send latest update's method (`POST, PUT, PATCH, DELETE`), path, collection and optional item id with WebSocket.

```json
{ "method": "PATCH", "path": "/api/users/2", "collection": "users", "itemId": 2 }
```

[wwwroot\index.html](https://github.com/ttu/dotnet-fake-json-server/blob/master/FakeServer/wwwroot/index.html) has a WebSocket example.

### CORS

CORS is enabled and it allows everything.

### Static Files

`GET /`

Returns static files from wwwroot. Default file is `index.html`.

### Swagger

Swagger is configured to endpoint `/swagger` and Swagger UI opens when project is started.

## Routes, Functionalities and Examples

```
GET    /
POST   /token
GET    /status
POST   /admin/reload

GET    /api
GET    /api/{collection}
POST   /api/{collection}
GET    /api/{collection}/{id}
PUT    /api/{collection}/{id}
PATCH  /api/{collection}/{id}
DELETE /api/{collection}/{id}

GET    /async/queue/{id}
DELETE /async/queue/{id}
POST   /async/{collection}
PUT    /async/{collection}/{id}
PATCH  /async/{collection}/{id}
DELETE /async/{collection}/{id}
```

Dynamic routes are defined by the name of item's collection and id: `api/{collection}/{id}`. All examples below use `users` as a collection name.

Asynchoronous operations follow [REST CookBook guide](http://restcookbook.com/Resources/asynchroneous-operations/). Updates will return `202` with location header to queue item. Queue will return `200` while job is processing and `303` when job is ready with location header to changed or new item.

For now API supports only id as the key field and integer as it's value type.

Method return values are specified [REST API Tutorial](http://www.restapitutorial.com/lessons/httpmethods.html).

### Status

Status endpoint returns current status of the service. 

```sh
$ curl http://localhost:57602/status
```
```json
{"status": "Ok"}
```

### Eager data reload

By default DataStore updates internal data on every query by reading data from the JSON file. 

`EagerDataReload` can be configured from `appsettings.json`.

```json
"Common": {
  "EagerDataReload": true
}
```

For performance reasons `EagerDataReload` can be changed to _false_. Then data is reloaded from the file only when DataStore is initialized and when data is updated. 

If `EagerDataReload` is _false_ and JSON file is updated manually, reload endpoint must be called if new data will be queried before any updates. 

### Reload

Reload endpoint can be used to reload JSON data from the file to DataStore. Endoint is in Admin controller, so it is usable also with Swagger.

```sh
$ curl -X POST http://localhost:57602/admin/reload --data ""
```

##### Example JSON data

Data used in examples

```json
{
  "users": [
    { "id": 1, "name": "Phil", "age": 40, "location": "NY" },
    { "id": 2, "name": "Larry", "age": 37, "location": "London" },
    { "id": 3, "name": "Thomas", "age": 40, "location": "London" }
  ],
  "movies": []
}
```

Example JSON generation guide used in unit tests [CreateJSON.md](CreateJson.md)

####  List collections 

```
GET /api

200 OK : List of collections
```

```sh
$ curl http://localhost:57602/api
```

```json
[ "users", "movies" ]
```

#### Get items

```
GET /api/{collection}

200 OK        : Collection is found
404 Not Found : Collection is not found or it is empty
```

Amount of items can be defined with `skip` and `take` parameters. By default request returns first 10 items.

```sh
$ curl http://localhost:57602/api/users
```

Example request returns items from 6 to 26.

```sh
$ curl http://localhost:57602/api/users?skip=5&take=20
```


#### Get items with query 

```
GET api/{collection}?field=value&otherField=value

200 OK        : Collection is found
404 Not Found : Collection is not found or it is empty
```

```sh
$ curl http://localhost:57602/api/users?age=40
```
```json
[ 
 { "id": 1, "name": "Phil", "age": 40, "location": "NY" },
 { "id": 3, "name": "Thomas", "age": 40, "location": "London" }
]
```

Query can have path to child properties. Property names are separated by periods.

`GET api/{collection}?parent.child.grandchild.field=value`

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
$ curl http://localhost:57602/api/users?employees.address.city=London
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

#### Get item with id 

``` 
GET /api/{collection}/{id}

200 OK          : Item is found
400 Bad Request : Collection is not found
404 Not Found   : Item is not found
```

```sh
$ curl http://localhost:57602/api/users/1
```

```json
{ "id": 1, "name": "Phil", "age": 40, "location": "NY" }
```

#### Get nested items

```
GET /api/{collection}/{id}/{restOfThePath}

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

#### Add item 

```
POST /api/{collection}

201 Created     : New item is created
400 Bad Request : New item is null
```

```sh
$ curl -H "Accept: application/json" -H "Content-type: application/json" -X POST -d '{ "name": "Phil", "age": 40, "location": "NY" }' http://localhost:57602/api/users/
```

Response has new item's id and Location header to new item

```json
{ "id": 6 }

Headers:
Location=/api/users/6
```

#### Replace item 

``` 
PUT /api/{collection}/{id}

204 No Content  : Item is replaced
400 Bad Request : Item is null
404 Not Found   : Item is not found
```

```sh
$ curl -H "Accept: application/json" -H "Content-type: application/json" -X PUT -d '{ "name": "Roger", "age": 28, "location": "SF" }' http://localhost:57602/api/users/1
```

#### Update item 

```
PATCH /api/{collection}/{id}

204 No Content  : Item updated
400 Bad Request : PATCH is empty
404 Not Found   : Item is not found
```

```sh
$ curl -H "Accept: application/json" -H "Content-type: application/json" -X PATCH -d '{ "name": "Timmy" }' http://localhost:57602/api/users/1
```

#### Delete item

``` 
DELETE /api/{collection}/{id}

204 No Content  : Item deleted
404 Not Found   : Item is not found
```

```sh
$ curl -X DELETE http://localhost:57602/api/users/1
```

### Async Operations

`/async` endoint has long running jobs for each update operation.

```
POST/PUT/PATCH/DELETE /async/{collection}/{id}

202 Accepted    : New job started
400 Bad Request : Job not started

Headers:
Location=http://{url}:{port}/async/queue/{id}
```

Update operations will return location to job queue in headers.

Create new item. Curl has a verbose flag so it will print response headers among other things.

```sh
$ curl -H "Accept: application/json" -H "Content-type: application/json" -X POST -d '{ "name": "Phil", "age": 40, "location": "NY" }' -v http://localhost:57602/async/users/
```

```
GET /async/queue/{id}

200 OK        : Job running
303 See Other : Job ready
404 Not Found : Job not found

Headers:
Location=http://{url}:{port}/api/{collectionId}/{id}
```

When Job is ready, status code will be redirect See Other. Location header will have modified item's url.

After job is finished, it must be deleted manually

```
DELETE /async/queue/{id}

200 OK        : Job deleted
404 Not Found : Job not found
```

##### Job delay

Delay for operations can be set from `appsettings.json`. With long delay it is easier to simulate long running jobs.

```json
  "Jobs": {
    "DelayMs": 2000
  }
 ```

Delay value is milliseconds. Default value is 2000ms.

### Simulate Delay and Random Errors

Delay and errors can be configured from `appsettings.json`.

Delay can be simulated by setting `Simulate.Delay.Enabled` to __true__. The inbound request is delayed. The length of the delay is randomly chosen between `MinMs`and `MaxMs`. Delay can be configured for only certain HTTP Methods, e.g. only POST updates have delay and all GET requests are handled normally.

```json
"Simulate": {
    "Delay": {
      "Enabled": true,
      "Methods": [ "GET", "POST", "PUT", "PATCH", "DELETE" ],
      "MinMs": 2000,
      "MaxMs": 5000
    }
}
```

Random errors can be simulated by setting `Simulate.Error.Enabled` to __true__. Error is thrown if set `Probability` is greater or equal to randomly chosen value between 1 and 100. Error can be configured for only certain HTTP Methods.

```json
"Simulate": {
    "Error": {
      "Enabled": true,
      "Methods": [ "POST", "PUT", "PATCH", "DELETE" ],
      "Probability": 50
    }
}
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
$ ab -p user.json -T application/json -c 10 -n 2000 http://localhost:57602/api/users
```

### License

Licensed under the [MIT](LICENSE) License.

