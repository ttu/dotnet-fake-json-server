.NET Fake JSON Server
--------------------------

[![Build Status](https://travis-ci.org/ttu/dotnet-fake-json-server.svg?branch=master)](https://travis-ci.org/ttu/dotnet-fake-json-server) [![Build status](https://ci.appveyor.com/api/projects/status/hacg7qupp5oxbct8?svg=true)](https://ci.appveyor.com/project/ttu/dotnet-fake-json-server)

Fake REST API for prototyping or as a CRUD backend with experimental GraphQL query support.

* No need to define types for resources. Types are handled dynamically
* No database. Data is stored to a JSON file
* CRUD operations (GET, PUT, POST, PATCH, DELETE)
* Async versions of update operations with long running jobs
* Simulate delay and errors for requests
* No configuration needed, start the Server and API is ready to be used with any data


## Features
 
* ASP.NET Core 2.0 / C# 7
* Can be used without .NET
  * [Docker](#docker) 
  * [Self-contained Application](#self-contained-application)
* Uses [JSON Flat File Data Store](https://github.com/ttu/json-flatfile-datastore)
  * All changes are automatically saved to defined JSON file
* Token and Basic Authentication
  * Add allowed usernames/passwords to `authentication.json`
* WebSockets
* Static files
* Swagger
* CORS
* _Experimental_ GraphQL query support

## Get started

Get source code from GitHub

```sh
$ git clone https://github.com/ttu/dotnet-fake-json-server.git
```

#### Start with .NET CLI

```sh
$ cd dotnet-fake-json-server/FakeServer
$ dotnet run [--file] [--urls]

# Optional arguments:
#   --file     Data store's JSON file (default datastore.json)
#   --urls     Server url (default http://localhost:57602)      

# Example: Start server
$ dotnet run --file data.json --urls http://localhost:57602
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

#### Self-contained Application

1) Go to [Lates Release](https://github.com/ttu/dotnet-fake-json-server/releases/latest)
1) Download correct archive matching your OS
1) Extract files and execute

Self-contained application archive contains Fake JSON Server and .NET Core runtime inculding all required third-party dependencies. __No installation or prerequisites are needed__.

E.g. download version _0.4.0_ for _macOS_

```sh
$ mkdir FakeServer && cd FakeServer
$ wget https://github.com/ttu/dotnet-fake-json-server/releases/download/0.4.0/fakeserver-osx-x64.tar.gz
$ tar -zxvf fakeserver-osx-x64.tar.gz
$ chmod +x FakeServer
$ ./FakeServer
```

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

#### Example project

[Redux TodoMVC example](https://github.com/ttu/todomvc-fake-server) modified to use Fake JSON Server as a Back End.

## Features

### Authentication

Fake REST API supports Token and Basic Authentication. 

Authentication can be disabled from `authentication.json` by setting Enabled to `false`. `AuthenticationType` options are `token` and `basic`.

```json
{
  "Authentication": {
    "Enabled": true,
    "AuthenticationType": "token",
    "Users": [
        { "Username": "admin", "Password": "root" }
    ]
  }
}
```

#### Token Authentication

API has a token provider middleware which provides an endpoint for token generation `/token`.

Get token:
```sh
$ curl -X POST -H 'content-type: multipart/form-data' -F username=admin -F password=root http://localhost:57602/token
```

Add token to Authorization header:
```sh
$ curl -H 'Authorization: Bearer [TOKEN]' http://localhost:57602/api
```

Check SimpleTokenProvider from [GitHub](https://github.com/nbarbettini/SimpleTokenProvider) and [StormPath's blog post](https://stormpath.com/blog/token-authentication-asp-net-core).

#### Basic Authentication

> NOTE: It is not recommended to use Basic Authentication in production

```sh
$ curl -u admin:root http://localhost:57602/api
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
GET      /
POST     /token
POST     /admin/reload

GET      /api
GET      /api/{collection}
POST     /api/{collection}
GET      /api/{collection}/{id}
PUT      /api/{collection}/{id}
PATCH    /api/{collection}/{id}
DELETE   /api/{collection}/{id}
OPTIONS  /api/*

GET      /async/queue/{id}
DELETE   /async/queue/{id}
POST     /async/{collection}
PUT      /async/{collection}/{id}
PATCH    /async/{collection}/{id}
DELETE   /async/{collection}/{id}
OPTIONS  /async/*

POST     /graphql
```

##### Routes

Dynamic routes are defined by the name of item's collection and id: `api/{collection}/{id}`. All examples below use `users` as a collection name.

If `/api` or `/async` are needed to change to something different, change `ApiRoute` or `AsyncRoute` from `Config.cs`.

```csharp
public class Config
{
    public const string ApiRoute = "api";
    public const string AsyncRoute = "async";
    public const string GraphQLRoute = "graphql";
}
```

For example, if `api`-prefix is not wanted in the route, then remove `api` from `ApiRoute`.

```csharp
public const string ApiRoute = "";
```

```sh
# Query with default route
$ curl 'http://localhost:57602/api/users?skip=5&take=20'
# Query with updated route
$ curl 'http://localhost:57602/users?skip=5&take=20'
```

##### Identifiers

`id` is used as the identifier field. By default Id field's type is _integer_. `POST` will always use integer as id field's type.

```json
"users":[
  { "id": 1 }
],
"sensors": [
  { "id": "E:52:F7:B3:65:CC" }
]
```

If _string_ is used as the identifiers type, then items must be inserted with `PUT` and  `UpsertOnPut` must be set to _true_ from `appsettings.json`.

##### Return codes

Asynchoronous operations follow [REST CookBook guide](http://restcookbook.com/Resources/asynchroneous-operations/). Updates will return `202` with location header to queue item. Queue will return `200` while job is processing and `303` when job is ready with location header to changed or new item.

Method return codes are specified in [REST API Tutorial](http://www.restapitutorial.com/lessons/httpmethods.html).

#### OPTIONS method

OPTIONS method will return `Allow` header with a list of HTTP methods that may be used on the resource.

```sh
$ curl -X OPTIONS -v http://localhost:57602/api/
```

```json
200 OK

Headers:
Allow: GET, POST, OPTIONS
```

#### Eager data reload

By default Data Store updates it's internal data on every request by reading the data from the JSON file. 

`EagerDataReload` can be configured from `appsettings.json`.

```json
"Common": {
  "EagerDataReload": true
}
```

For performance reasons `EagerDataReload` can be changed to _false_. Then the data is reloaded from the file only when Data Store is initialized and when the data is updated. 

If `EagerDataReload` is _false_ and JSON file is updated manually, reload endpoint must be called if new data will be queried before any updates. 

#### Reload

Reload endpoint can be used to reload JSON data from the file to Data Store. Endoint is in Admin controller, so it is usable also with Swagger.

```sh
$ curl -X POST http://localhost:57602/admin/reload --data ""
```

### Endpoints

##### Example JSON data

Data used in example requests, unless otherwise stated:

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

Example JSON generation guide for data used in unit tests [CreateJSON.md](CreateJson.md).

####  List collections 

```
> GET /api

200 OK : List of collections
```

```sh
$ curl http://localhost:57602/api
```

```json
[ "users", "movies" ]
```

#### Query

```
> GET /api/{collection}

200 OK          : Collection is found
400 Bad Request : Invalid query parameters
404 Not Found   : Collection is not found or it is empty
```

By default request returns results in and array. Headers have the collection's total item count (`X-Total-Count`) and pagination links (`Link`).

```sh
$ curl http://localhost:57602/api/users
```
```json
[
  { "id": 1, "name": "Phil", "age": 40, "location": "NY" },
  { "id": 2, "name": "Larry", "age": 37, "location": "London" },
  { "id": 3, "name": "Thomas", "age": 40, "location": "London" },
  ...
]

Headers:
X-Total-Count=20
Link=
<http://localhost:57602/api/users?offset=15&limit=5>; rel="next",
<http://localhost:57602/api/users?offset=15&limit=5>; rel="last",
<http://localhost:57602/api/users?offset=0&limit=5>; rel="first",
<http://localhost:57602/api/users?offset=5&limit=5>; rel="prev"
```

The return value can also be a JSON object. Set `UseResultObject` to _true_ from `appsettings.json`.

```json
"Api": {
  "UseResultObject": true
}
```

JSON object has items in results array in result field, link object has the pagination info, skip, take and total count fields.

```json
{
  "results": [
    ...
  ],
  "link": {
    "Prev": "http://localhost:57602/api/users?offset=5&limit=5",
    "Next": "http://localhost:57602/api/users?offset=15&limit=5",
    "First": "http://localhost:57602/api/users?offset=0&limit=5",
    "Last": "http://localhost:57602/api/users?offset=15&limit=5"
  },
  "offset": 10,
  "limit": 5,
  "count": 20
}
```

##### Slice

Slicing can be defined with `skip` and `take` or `offset` and `limit` parameters. By default request returns first 512 items.

Example request returns items from 6 to 26.

```sh
# skip and take
$ curl 'http://localhost:57602/api/users?skip=5&take=20'
# offset and limit
$ curl 'http://localhost:57602/api/users?offset=5&limit=20'
```

##### Pagination headers

Link items are optional, so e.g. if requested items are starting from index 0, then prev page and first page link won't be added to the Link header.

Headers follow [GitHub Developer](https://developer.github.com/v3/guides/traversing-with-pagination/) guide.

#### Filter

```
> GET api/{collection}?field=value&otherField=value
```

```sh
$ curl 'http://localhost:57602/api/users?age=40'
```
```json
[ 
 { "id": 1, "name": "Phil", "age": 40, "location": "NY" },
 { "id": 3, "name": "Thomas", "age": 40, "location": "London" }
]
```

##### Child properties

Query can have a path to child properties. Property names are separated by periods.

````
> GET api/{collection}?parent.child.grandchild.field=value
```` 

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

##### Filter operators

Query filter can include operators. Operator identifier is added to the end of the field.

```
> GET api/{collection}?field{operator}=value

=     : Equal to
_ne=  : Not equal
_lt=  : Less than
_gt=  : Greater than
_lte= : Less than or equal to
_gte= : Greater than or equal to
```

Query users with age less than 40.

```sh
$ curl http://localhost:57602/api/users?age_lt=40
```
```json
[ 
  { "id": 2, "name": "Larry", "age": 37, "location": "London" }
]
```

##### Full-text search

Full-text search can be performed with the `q`-parameter followed by search text. Search is not case sensitive.

```
> GET api/{collection}?q={text}
```` 

Get all users that contain text London in the value of any of it's properties.

```sh
$ curl http://localhost:57602/api/users?q=london
```

##### Select Fields

Choose which fields to include in the results. Field names are separated by comma.

```
> GET api/{collection}?fields={fields}
```` 

Select age and name from users.

```sh
$ curl http://localhost:57602/api/users?fields=age,name
```

```json
[ 
  { "name": "Phil", "age": 40 },
  { "name": "Larry", "age": 37 }
]
```

#### Get item with id 

``` 
> GET /api/{collection}/{id}

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
> GET /api/{collection}/{id}/{restOfThePath}

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
> POST /api/{collection}

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
Location=http://localhost:57602/api/users/6
```

#### Replace item 

``` 
> PUT /api/{collection}/{id}

204 No Content  : Item is replaced
400 Bad Request : Item is null
404 Not Found   : Item is not found
```

```sh
$ curl -H "Accept: application/json" -H "Content-type: application/json" -X PUT -d '{ "name": "Roger", "age": 28, "location": "SF" }' http://localhost:57602/api/users/1
```

#### Update item 

```
> PATCH /api/{collection}/{id}

204 No Content  : Item updated
400 Bad Request : PATCH is empty
404 Not Found   : Item is not found
```

```sh
$ curl -H "Accept: application/json" -H "Content-type: application/json" -X PATCH -d '{ "name": "Timmy" }' http://localhost:57602/api/users/1
```

#### Delete item

``` 
> DELETE /api/{collection}/{id}

204 No Content  : Item deleted
404 Not Found   : Item is not found
```

```sh
$ curl -X DELETE http://localhost:57602/api/users/1
```

### Async Operations

`/async` endoint has long running jobs for each update operation.

```
> POST/PUT/PATCH/DELETE /async/{collection}/{id}

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
> GET /async/queue/{id}

200 OK        : Job running
303 See Other : Job ready
404 Not Found : Job not found

Headers:
Location=http://{url}:{port}/api/{collectionId}/{id}
```

When Job is ready, status code will be redirect See Other. Location header will have modified item's url.

After job is finished, it must be deleted manually

```
> DELETE /async/queue/{id}

204 No Content : Job deleted
404 Not Found  : Job not found
```

##### GraphQL query support

GraphQL implementation is experimental and supports only most basic queries. At the moment this is a good way to compare simple GraphQL and REST queries.

```
> POST /graphql

Content-type: application/graphql
Body: [query]


200 OK              : Query successful 
400 Bad Request     : Query contains errors
501 Not Implemented : HTTP method and/or content-type combination not implemented
```

Response is in JSON format. It contains `data` and `errors` fields. `errors` field is not present if there are no errors. 
```json
{
  "data": { 
    "users": [ ... ],
    ...
  },
  "errors": [ ... ]
}
```` 

Implementation supports equals filtering with arguments. Query's first field is the name of the collection.

```graphql
query {
  [collection](filter: value) {
    [field1]
    [field2](filter: value) {
      [field2.1]
    }
    [field3]
  }
}
```

Get `familyName` and `age` of the `children` from `families` where `id` is 1 and `name`from all `users`.

```sh
$ curl -H "Content-type: application/graphql" -X POST -d '{ families(id: 1) { familyName children { age } } users { name } }' http://localhost:57602/graphql
```


Implementaiton accepts queries with operation type, with any query name (which is ignored) and query shorthands.

```graphql
# Operation type
query {
  users(id: 3) {
    name
    work {
      location
    }
  }
}

# Optional query name
query getUsers {
  users {
    name
    work {
      location
    }
  }
}

# Query shorthand
{
  families {
    familyName
    childred(age: 5){
        name
    }
  }
}
```

Uses [graphql-dotnet](https://github.com/graphql-dotnet/graphql-dotnet) to parse Abstract Syntax Tree from the query.

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

Delay can be simulated by setting `Simulate.Delay.Enabled` to _true_. The inbound request is delayed. The length of the delay is randomly chosen between `MinMs`and `MaxMs`. Delay can be configured for only certain HTTP Methods, e.g. only POST updates have delay and all GET requests are handled normally.

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

Random errors can be simulated by setting `Simulate.Error.Enabled` to _true_. Error is thrown if set `Probability` is greater or equal to randomly chosen value between 1 and 100. Error can be configured for only certain HTTP Methods.

```json
"Simulate": {
    "Error": {
      "Enabled": true,
      "Methods": [ "POST", "PUT", "PATCH", "DELETE" ],
      "Probability": 50
    }
}
```

Error simulation is always skipped for Swagger, WebSocket (ws) and for any html file.

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

## Get specific version

Releases are marked with Tag. 

At the moment there is no compiled versions of the releases available.

```sh
# List versions
$ git tag -l
# Checkout specific tag 
$ git checkout tags/{version}
```

## Guidelines

Api uses best practices and recommendations from these guides:

* [REST CookBook](http://restcookbook.com/Resources/asynchroneous-operations/)
* [REST API Tutorial](http://www.restapitutorial.com/lessons/httpmethods.html)
* [Zalando Restful API Guidelines](https://zalando.github.io/restful-api-guidelines)
* [Microsoft API Design](https://docs.microsoft.com/en-us/azure/architecture/best-practices/api-design)
* [GitHub v3 Guide](https://developer.github.com/v3/guides/)
* [Introduction to GraphQL](http://graphql.org/learn/)

## Changelog

[Changelog](CHANGELOG.md)

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

## License

Licensed under the [MIT](LICENSE) License.

