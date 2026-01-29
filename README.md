Fake JSON Server
--------------------------

[![NuGet](https://img.shields.io/nuget/v/FakeServer.svg)](https://www.nuget.org/packages/FakeServer/)

| Build server | Platform       | Build status |
|--------------|----------------|-------------|
| Travis       | Linux / macOS  |[![Build Status](https://app.travis-ci.com/ttu/dotnet-fake-json-server.svg?branch=master)](https://app.travis-ci.com/ttu/dotnet-fake-json-server)| 
| CircleCI     | Windows        |[![CircleCI](https://dl.circleci.com/status-badge/img/gh/ttu/dotnet-fake-json-server/tree/master.svg?style=svg)](https://dl.circleci.com/status-badge/redirect/gh/ttu/dotnet-fake-json-server/tree/master)|

Fake JSON Server is a Fake REST API that can be used as a Back End for prototyping or as a template for a CRUD Back End. Fake JSON Server also has an experimental GraphQL query and mutation support.

* No need to define types for resources, uses dynamic typing
* No need to define routes, routes are handled dynamically
* No database, data is stored to a single JSON file
* No setup required, just start the server and API is ready to be used with any data

### Why to use this?

1) API is built following the best practices and can be used as a reference when building your own API
1) Contains all common features used with well functioning APIs (see features listed below)
1) Can be run on Windows, Linux and macOS without any installation or prerequisites from executable or with Docker

### Docs website

[https://ttu.github.io/dotnet-fake-json-server/](https://ttu.github.io/dotnet-fake-json-server/)

---

### Features

* Supported HTTP methods [#](#routes-functionalities-and-examples)
  * All methods for CRUD operations (_GET, PUT, POST, PATCH, DELETE_)
  * Methods for fetching resource information (_HEAD_, _OPTIONS_)
* Async versions of update operations with long running operations and queues [#](#async-operations)
* REST API follows best practices from multiple guides 
  * Uses correct Status Codes, Headers, etc.
  * As all guides have slightly different recommendations, this compilation is based on our opinions
* Paging, filtering, selecting, text search etc. [#](#slice)
* Token, Basic and API key Authentication [#](#authentication)
* WebSocket update notifications [#](#websockets)
* Simulate delay and errors for requests [#](#simulate-delay-and-random-errors)
* Static files [#](#static-files)
* Swagger [#](#swagger)
* CORS [#](#cors)
* Content Negotiation (output formats _JSON_, _CSV_ and _XML_) [#](#content-negotiaton)
* Caching and avoiding mid-air collisions with ETag [#](#caching-and-avoiding-mid-air-collisions-with-etag)
* Configurable custom response transformation [#](#custom-response-transformation)
* _Experimental_ GraphQL query and mutation support [#](#graphql)

### Developed with
 
* .NET 8
* ASP.NET Core
* Data is stored to a JSON-file with [JSON Flat File Data Store](https://github.com/ttu/json-flatfile-datastore)


## Table of contents
<details>
<summary>Click to here to see contents </summary>

- [Get started](#get-started)
    + [.NET CLI](#net-cli)
    + [Dotnet global tool](#dotnet-global-tool)
    + [Docker](#docker)
    + [Self-contained application](#self-contained-application)
    + [Serve static files](#serve-static-files)
- [Examples](#examples)
    + [Quick example](#quick-example)
    + [Example project](#example-project)
    + [Example queries](#example-queries)
- [Features](#features-1)
  * [Authentication](#authentication)
    + [Token authentication](#token-authentication)
    + [Basic authentication](#basic-authentication)
    + [API key authentication](#api-key-authentication)
  * [WebSockets](#websockets)
  * [CORS](#cors)
  * [Static files](#static-files)
  * [Swagger](#swagger)
  * [Caching and avoiding mid-air collisions with ETag](#caching-and-avoiding-mid-air-collisions-with-etag)
    + [Caching of unchanged resources](#caching-of-unchanged-resources)
    + [Avoiding mid-air collisions](#avoiding-mid-air-collisions)
  * [Content Negotiaton](#content-negotiaton)
  * [Simulate Delay and Random Errors](#simulate-delay-and-random-errors)
  * [Configurable Custom Response Transformation](#custom-response-transformation)
  * [Logging](#logging)
- [Routes, Functionalities and Examples](#routes--functionalities-and-examples)
    + [Collections and objects](#collections-and-objects)
    + [Routes](#routes)
    + [Identifiers](#identifiers)
    + [HTTP return codes](#http-return-codes)
    + [Data Store Id-field name](#data-store-id-field-name)
    + [Eager data reload](#eager-data-reload)
    + [Reload](#reload)
    + [Health Check](#health-check)
  * [Endpoints](#endpoints)
    + [JSON data used in examples](#json-data-used-in-examples)
    + [List collections (GET)](#list-collections-get)
    + [Query items (GET)](#query-items-get)
      - [Slice](#slice)
        - [Pagination headers](#pagination-headers)
      - [Filter](#filter)
        - [Filter operators](#filter-operators)
        - [Child properties](#child-properties)
      - [Full-text search](#full-text-search)
      - [Select Fields](#select-fields)
    + [Get item with id (GET)](#get-item-with-id-get)
      - [Get nested items](#get-nested-items)
    + [Add item (POST)](#add-item-post)
    + [Replace item (PUT)](#replace-item-put)
    + [Update item (PATCH)](#update-item-patch)
    + [Delete item (DELETE)](#delete-item-delete)
  * [OPTIONS method](#options-method)
  * [HEAD method](#head-method)
  * [Async Operations](#async-operations)
      - [Job delay](#job-delay)
  * [GraphQL](#graphql)
    + [Query](#query-1)
    + [Mutation](#mutation)
      - [Add item](#add-item-1)
      - [Update Item](#update-item)
      - [Replace item](#replace-item-1)
      - [Delete item](#delete-item-1)
- [Guidelines](#guidelines)
- [Other Links](#other-links)
- [Releases](#releases)
- [Changelog](#changelog)
- [Contributing](#contributing)
- [License](#license)

</details>

## Get started

### .NET CLI

```sh
# Get source code from GitHub
$ git clone https://github.com/ttu/dotnet-fake-json-server.git

$ cd dotnet-fake-json-server/FakeServer
$ dotnet run
```

Start server with defined data-file and url (optional arguments)

```sh
# Optional arguments:
#   --file <FILE>    Data store's JSON file (default datastore.json)
#   --urls <URL>     Server url (default http://localhost:57602)      
#   --serve <PATH>   Serve static files (default wwwroot)
#   --version        Prints the version of the app

$ dotnet run --file data.json --urls http://localhost:57602
```

### Dotnet global tool
 
Server can be installed as a [dotnet global tool](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools). Settings files are then located at `%USERPROFILE%\.dotnet\tools` (_Windows_) and `$HOME/.dotnet/tools` (_Linux/macOS_). By default data stores's JSON file will be created to execution directory.

```sh
# install as a global tool
$ dotnet tool install --global FakeServer

# Example: Start server
$ fake-server --file data.json --urls http://localhost:57602

# Update to the newest version
$ dotnet tool update --global FakeServer
```

### Docker

If you don't have .NET installed, you can run the server with Docker.

```sh
# Get source code from GitHub
$ git clone https://github.com/ttu/dotnet-fake-json-server.git

$ cd dotnet-fake-json-server
$ docker build -t fakeapi .

# Run in foreground
$ docker run -it -p 57602:57602 --name fakeapi fakeapi

# Run in detached mode (run in background)
$ docker run -it -d -p 57602:57602 --name fakeapi fakeapi

# Start stopped container (remove -a to run in background)
$ docker start -a fakeapi
```

Copy JSON-file to/from container. Filename is `datastore.json`

```sh
# Copy file from host to container
$ docker cp datastore.json fakeapi:/app/datastore.json

# Copy file from container to host
$ docker cp fakeapi:/app/datastore.json datastore.json
```

### Self-contained application

The self-contained application archive contains Fake JSON Server, .NET runtime and all required third-party dependencies. __No installation or prerequisites are needed__.

1) Go to [Latest Release](https://github.com/ttu/dotnet-fake-json-server/releases/latest)
1) Download correct archive matching your OS
1) Extract files and execute

E.g. download and execute version _0.11.0_ for _macOS_

```sh
$ mkdir FakeServer && cd FakeServer
$ wget https://github.com/ttu/dotnet-fake-json-server/releases/download/0.11.0/fakeserver-osx-x64.tar.gz
$ tar -zxvf fakeserver-osx-x64.tar.gz
$ chmod +x FakeServer
$ ./FakeServer
```

### Serve static files

Fake Server can serve static files. Location of files can be absolute or relative to the current location.

```sh
$ dotnet run -s/--serve [fullpath/relative path]
# e.g.
$ dotnet run -s build

# Use Fake Server as a global tool
$ fake-server -s/--serve [fullpath/relative path]]
# e.g.
$ fake-server --serve c:\temp\react_app\build
$ fake-server --serve /home/user/app/dist
$ fake-server --serve ./build
```

When user defines static files, it is assumed that user is serving a single page app and then REST API is not working. If API is needed, start other instance of Fake Server.

## Examples

### Quick example

```sh
# List collections (should be empty, if data.json didn't exist before)
$ curl http://localhost:57602/api

# Insert new user
$ curl -H "Content-type: application/json" -X POST -d '{ "name": "Phil", "age": 20, "location": "NY" }' http://localhost:57602/api/users/

# Insert another user
$ curl -H "Content-type: application/json" -X POST -d '{ "name": "James", "age": 40, "location": "SF" }' http://localhost:57602/api/users/

# List users
$ curl http://localhost:57602/api/users

# List users from NY
$ curl http://localhost:57602/api/users?location=NY

# Get User with Id 1
$ curl http://localhost:57602/api/users/1

# ...

# Add users to data.json manually

# Get all users
$ curl http://localhost:57602/api/users/
# ...

# Or open url http://localhost:57602/swagger/ with browser and use Swagger
```

### Example project

[Redux TodoMVC example](https://github.com/ttu/todomvc-fake-server) modified to use Fake JSON Server as a Back End.

### Example queries

Example queries are available in [Postman](https://www.postman.com/) workspace format in [FakeServer_Workspace.json](https://github.com/ttu/dotnet-fake-json-server/blob/master/docs/FakeServer_Workspace.json). These queries can be used in [Postman](https://www.postman.com/), [Insomnia](https://insomnia.rest/), [Yaak](https://yaak.app/), and similar tools.

## Features

### Authentication

Fake REST API supports Token and Basic authentication and API keys. 

Authentication can be disabled from `appsettings.json` by setting Enabled to `false`. `AuthenticationType` options are `token`, `basic` and `apikey`.

Add allowed usernames/passwords to `Users`-array. Add optional API key to `ApiKey`-property.

```json
"Authentication": {
  "Enabled": true,
  "AuthenticationType": "token",
  "Users": [
      { "Username": "admin", "Password": "root" }
  ],
  "ApiKey": "abcd1234"
}
```

#### Token authentication

API has a token provider middleware which provides an endpoint for token generation `/token`. Endpoint supports `'content-type: multipart/form-data` and `content-type: application/json`. Username and password must be in `username` and `password` fields.

Get token:

```sh
# content-type: multipart/form-data
$ curl -X POST -H 'content-type: multipart/form-data' -F username=admin -F password=root http://localhost:57602/token

# content-type: application/json
$ curl -X POST -H 'content-type: application/json' -d '{ "username": "admin", "password": "root" }' http://localhost:57602/token
```

Token can be fetch also using `Client Credentials` grant type (see example from Insomnia workspace):
```sh
$ curl -X POST -d "grant_type=client_credentials&client_id=admin&client_secret=root" http://localhost:57602/token
```

Add token to Authorization header:

```sh
$ curl -H 'Authorization: Bearer [TOKEN]' http://localhost:57602/api
```

Token authentication supports logout functionality. By design, tokens do not support token invalidation and logout is implemented by blacklisting tokens.

```sh
$ curl -X POST -d '' -H 'Authorization: Bearer [TOKEN]' http://localhost:57602/logout
```

The implementation is quite similiar to SimpleTokenProvider and more info on that can be found from [GitHub](https://github.com/nbarbettini/SimpleTokenProvider) and [StormPath's blog post](https://stormpath.com/blog/token-authentication-asp-net-core).

#### Basic authentication

> NOTE: It is not recommended to use Basic Authentication in production as base64 is a reversible encoding

Add base64 encoded username:password to authorization header e.g. `'Authorization: Basic YWRtaW46cm9vdA=='`.

```sh
$ curl -u admin:root http://localhost:57602/api
# -u argument creates Authorization header with encoded username and password
$ curl -H 'Authorization: Basic YWRtaW46cm9vdA==' http://localhost:57602/api
```

#### API key authentication

Add key set to Authentication settings to `X-API-KEY` header e.g. `X-API-KEY: abcd1234'`.

```sh
$ curl -H 'X-API-KEY: abcd1234' http://localhost:57602/api
```

### WebSockets

API will send the latest update's method (`POST, PUT, PATCH, DELETE`), path, collection and optional item id with WebSocket.

```json
{ "method": "PATCH", "path": "/api/users/2", "collection": "users", "itemId": 2 }
```

[wwwroot\index.html](https://github.com/ttu/dotnet-fake-json-server/blob/master/FakeServer/wwwroot/index.html) has a WebSocket example.

### CORS

CORS is enabled and it allows everything.

### Static files

`GET /`

Returns static files from wwwroot or defined location. Default file is `index.html`.

Check [how to serve static files](#serve-static-files) from defined location.

### Swagger

Swagger is configured to endpoint `/swagger` and Swagger UI opens when project is started from IDE.

### Caching and avoiding mid-air collisions with ETag

Caching can be disabled from `appsettings.json` by setting ETag.Enabled to `false`.

```json
"Caching": {
  "ETag": { 
    "Enabled": true 
  }
}
```

If caching is enabled, _ETag_ is added to response headers.

```sh
$ curl -v 'http://localhost:57602/api/users?age=40'
```

```txt
200 OK

Headers:
ETag: "5yZCXmjhk5ozJyTK4-OJkkd_X18"
```

#### Caching of unchanged resources

If a request contains the `If-None-Match` header, the header's value is compared to the response's body and if the value matches to the body's checksum then `304 Not Modified` is returned.

```sh
$ curl -H "If-None-Match: \"5yZCXmjhk5ozJyTK4-OJkkd_X18\"" 'http://localhost:57602/api/users?age=40'
```

#### Avoiding mid-air collisions

If the `PUT` request contains the `If-Match` header, the header's value is compared to the item to be updated. If the value matches to the item's checksum then items is updated, else `412 Precondition Failed` is returned.

### Content negotiaton

Client can determine what type of representation is desired with `Accept` header. By default data is returned in JSON (`text/json`, `application/json`).

Supported types are _JSON_, _CSV_ and _XML_.

```
text/json
application/json
text/csv
text/xml
application/xml
```

Get all users in _CSV_

```sh
$ curl -H "Accept: text/csv" http://localhost:57603/api/users
```

If content types is not supported `406 Not Acceptable` is returned.

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

### Custom Response Transformation

Fake Server has a custom response middleware to transform reponse body with C#-scripts.

Multiple scripts can be configured and if path matches multiple scipts, last match will be used.

```json
"CustomResponse": {
    "Enabled": true,
    "Scripts": [
        {
            "Script": "return new { Data = _Body, Success = _Context.Response.StatusCode == 200 };",
            "Methods": [ "GET" ],
            "Paths": [ "api" ],
            "Usings": [ "System", "Microsoft.AspNetCore.Http" ],
            "References": [ "Microsoft.AspNetCore" ]
        },
        {
            "Script": "return new { Data = \"illegal operation\" };",
            "Methods": [ "GET" ],
            "Paths": [ "api/users" ],
            "Usings": [ "System", "Microsoft.AspNetCore.Http" ],
            "References": [ "Microsoft.AspNetCore" ]
      }
    ]
}
```

C# code is executed as a csscript and it has some special reacy processed objects.

```csharp
// HttpContext
public HttpContext _Context;
// Collection id parsed from the Request path
public string _CollectionId;
// Original Response Body encoded to string
public string _Body;
// Request Http Method
public string _Method;
```

Example script creates new anonymous object
```csahrp
return new { Data = _Body, Success = _Context.Response.StatusCode == 200 };
```

Previous script will have a response body:

```txt
{
  "Data": [
    { "id": 1, "name": "James" ...},
    { "id": 2, "name": "Phil", ... },
    ...
  ],
  "Success": true
}
```

If response data requires so dynamically named properties, e.g. `users` in the example, then response requires more complex processing.

```txt
{
  "Data": {
    "users": [
      { "id": 1, "name": "James" ...},
      { "id": 2, "name": "Phil", ... },
      ...
    ]
  },
  "Success": true
}
```

C#-code for the processing would be following:

```csharp
var data = new ExpandoObject();
var dataItems = data as IDictionary<string, object>;
dataItems.Add(_CollectionId, _Body);

var body = new ExpandoObject();
var items = body as IDictionary<string, object>;
items.Add("Data", data);
items.Add("Success", _Context.Response.StatusCode == 200);
return body;
```

Script also would need `System.Collections.Generic` and `System.Dynamic` as imports. 

```json
{
    "Script": "var data = new ExpandoObject();var dataItems = data as IDictionary<string, object>;dataItems.Add(_CollectionId, _Body);var body = new ExpandoObject();var items = body as IDictionary<string, object>;items.Add(\"Data\", data);items.Add(\"Success\", _Context.Response.StatusCode == 200);return body;",
    "Methods": [ "GET" ],
    "Paths": [ "api" ],
    "Usings": [ "System", "System.Dynamic", "System.Collections.Generic", "Microsoft.AspNetCore.Http" ],
    "References": [ "Microsoft.AspNetCore" ]
}
```

### Logging

Fake JSON Server writes a log file to the application base path (execution folder).

Console logging can be enabled from `appsettings.json` by adding a new item to _Serilog.WriteTo_-array.

```json
"Serilog": {
  "WriteTo": [
    { "Name": "File" },
    { "Name": "Console" }
  ]
}
```

## Routes, functionalities and examples

```
GET      /
POST     /token
POST     /logout
POST     /admin/reload
GET      /health

GET      /api
HEAD     /api
GET      /api/{collection/object}
HEAD     /api/{collection/object}
POST     /api/{collection}
GET      /api/{collection}/{id}
HEAD     /api/{collection}/{id}
PUT      /api/{collection}/{id}
PATCH    /api/{collection}/{id}
DELETE   /api/{collection}/{id}
PUT      /api/{object}
PATCH    /api/{object}
DELETE   /api/{object}
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

### Collections and objects

Fake JSON Server supports both collections (arrays) and single objects at the root level.

#### Collections

Collections are arrays of items that support full CRUD operations with auto-generated IDs:

```json
{
  "users": [
    { "id": 1, "name": "Phil", "age": 40 },
    { "id": 2, "name": "Larry", "age": 37 }
  ],
  "posts": []
}
```

#### Single Objects

Single objects are handled differently - they don't have IDs and support only GET, PUT, PATCH, and DELETE operations:

```json
{
  "users": [...],
  "configuration": {
    "ip": "192.168.0.1"
  }
}
```

**Single Object Operations:**

```sh
# Get single object
$ curl http://localhost:57602/api/configuration

# Update single object (replaces entire object)
$ curl -X PUT -H "Content-type: application/json" \
  -d '{"apiUrl": "https://new-api.com", "timeout": 3000}' \
  http://localhost:57602/api/configuration

# Partially update single object
$ curl -X PATCH -H "Content-type: application/json+merge-patch" \
  -d '{"timeout": 8000}' \
  http://localhost:57602/api/configuration

# Delete single object (sets to null)
$ curl -X DELETE http://localhost:57602/api/configuration
```

**Key Differences:**
- Collections: Support POST (create), GET, PUT, PATCH, DELETE with auto-generated IDs
- Single Objects: Support GET, PUT, PATCH, DELETE only (no POST, no IDs)
- Single objects return the object directly, not wrapped in an array
- DELETE on single objects sets the value to `null` instead of removing the item


### Routes

Dynamic routes are defined by the name of item's collection and id: `api/{collection}/{id}`. All examples below use `users` as a collection name.

If `/api` or `/async` are needed to change to something different, change `ApiRoute` or `AsyncRoute` from `Config.cs`.

```csharp
public class Config
{
    public const string ApiRoute = "api";
    public const string AsyncRoute = "async";
    public const string GraphQLRoute = "graphql";
    public const string TokenRoute = "token";
    public const string TokenLogoutRoute = "logout";
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

### Identifiers

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

### HTTP return codes

Method return codes are specified in [REST API Tutorial](http://www.restapitutorial.com/lessons/httpmethods.html).

Asynchoronous operations follow the [REST CookBook guide](http://restcookbook.com/Resources/asynchroneous-operations/). Updates will return `202` with location header to queue item. Queue will return `200` while operation is processing and `303` when job is ready with location header to changed or new item.

### Data Store Id-field name

Name of the Id-field used by Data Store can be configure from `appsettings.json`. Default name for the id-field is `id`.

```json
"DataStore": {
  "IdField": "id"
}
```

### Eager data reload

By default Data Store updates its internal data on every request by reading the data from the JSON file. 

`EagerDataReload` can be configured from `appsettings.json`.

```json
"DataStore": {
  "EagerDataReload": true
}
```

For performance reasons `EagerDataReload` can be changed to _false_. Then the data is reloaded from the file only when Data Store is initialized and when the data is updated. 

If `EagerDataReload` is _false_ and JSON file is updated manually, reload endpoint must be called if new data will be queried before any updates. 

### Reload

Reload endpoint can be used to reload JSON data from the file to Data Store. Endpoint is in Admin controller, so it is usable also with Swagger.

```sh
$ curl -X POST http://localhost:57602/admin/reload --data ""
```

### Health Check

The health check endpoint provides status information about the Fake JSON Server. It monitors critical dependencies (like the data store) and reports service status, uptime, and version information.

```
> GET /health

200 OK                        : Application is healthy
503 Service Unavailable       : Application is unhealthy (data store is inaccessible)
```

The response contains:
- `status`: "Healthy" when all services are operational, "Unhealthy" otherwise
- `uptime`: Duration since application startup in days, hours, and minutes (only when healthy)
- `version`: Application version number

Example of a healthy response:

```sh
$ curl http://localhost:57602/health
```

```json
{
  "status": "Healthy",
  "uptime": "2 days, 5 hours, 30 minutes",
  "version": "1.0.0"
}
```

Example of an unhealthy response:

```json
{
  "status": "Unhealthy",
  "version": "1.0.0"
}
```

This endpoint is useful for:
- Monitoring service health in production environments
- Integration with container orchestration systems for health probes
- Checking if the data store is accessible

### Endpoints

#### JSON data used in examples

Data used in example requests, unless otherwise stated:

```json
{
  "users": [
    { "id": 1, "name": "Phil", "age": 40, "location": "NY" },
    { "id": 2, "name": "Larry", "age": 37, "location": "London" },
    { "id": 3, "name": "Thomas", "age": 40, "location": "London" }
  ],
  "movies": [],
  "configuration": { "ip": "192.168.0.1" }
}
```

Note: `users` and `movies` are collections (arrays), while `configuration` is a single object.

Example JSON generation guide for data used in unit tests [CreateJSON.md](docs/CreateJson.md).

#### List collections (GET)

```txt
> GET /api

200 OK : List of collections
```

Get all collections.

```sh
$ curl http://localhost:57602/api
```

```json
[ "users", "movies", "configuration" ]
```

#### Query items (GET)

```
> GET /api/{collection}

200 OK          : Collection is found
400 Bad Request : Invalid query parameters
404 Not Found   : Collection is not found
```

By default the request returns results in an array. Headers have the collection's total item count (`X-Total-Count`) and pagination links (`Link`).

```sh
$ curl http://localhost:57602/api/users
```
```txt
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
  "results": [],
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

Single object doesn't support result object. If the endpoint is a single object, only item object is returned. 

```sh
$ curl http://localhost:57602/api/configuration
```

```json
{ "ip": "192.168.0.1" }
```

##### Slice

Slicing can be defined with `skip`/`take`, `offset`/`limit` or `page`/`per_page` parameters. By default request returns the first 512 items.

Example request returns items from 11 to 20.

```sh
# skip and take
$ curl 'http://localhost:57602/api/users?skip=10&take=10'
# offset and limit
$ curl 'http://localhost:57602/api/users?offset=10&limit=10'
# page and per_page
$ curl 'http://localhost:57602/api/users?page=2&per_page=10'
```

###### Pagination headers

Link items are optional, so e.g. if requested items are starting from index 0, then the prev and first page link won't be added to the Link header.

Headers follow [GitHub Developer](https://developer.github.com/v3/guides/traversing-with-pagination/) guide.

##### Sort

```
> GET api/{collection}?sort=[+/-]field,[+/-]otherField
```

Sort contains comma-spearetd list of fields defining the sort. Sort direction can be specified with `+` (_ascending_) or `-` (_descending, default_) prefix.

Get all users sorted by `location` (_descending_) and then by `age` (_ascending_).

```sh
$ curl 'http://localhost:57602/api/users?sort=location,+age'
```

```json
[
  { "id": 2, "name": "Larry", "age": 37, "location": "London" },
  { "id": 3, "name": "Thomas", "age": 40, "location": "London" },
  { "id": 1, "name": "Phil", "age": 40, "location": "NY" }
]
```

##### Filter

```
> GET api/{collection}?field=value&otherField=value
```

Get all users whose `age` equals to _40_.

```sh
$ curl 'http://localhost:57602/api/users?age=40'
```

```json
[ 
 { "id": 1, "name": "Phil", "age": 40, "location": "NY" },
 { "id": 3, "name": "Thomas", "age": 40, "location": "London" }
]
```

###### Filter operators

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

Query users with `age` less than _40_.

```sh
$ curl http://localhost:57602/api/users?age_lt=40
```
```json
[ 
  { "id": 2, "name": "Larry", "age": 37, "location": "London" }
]
```

###### Filter with child properties

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

Get all companies which has employees with _London_ in `address.city`.

```sh
$ curl http://localhost:57602/api/companies?employees.address.city=London
```

Query will return ACME from the example JSON.

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







##### Full-text search

Full-text search can be performed with the `q`-parameter followed by search text. Search is not case sensitive.

```
> GET api/{collection}?q={text}
```` 

Get all users that contain text _London_ in the value of any of it's properties.

```sh
$ curl http://localhost:57602/api/users?q=london
```

##### Select fields

Choose which fields to include in the results. Field names are separated by comma.

```
> GET api/{collection}?fields={fields}
```` 

Select `age` and `name` from users.

```sh
$ curl http://localhost:57602/api/users?fields=age,name
```

```json
[ 
  { "name": "Phil", "age": 40 },
  { "name": "Larry", "age": 37 }
]
```

#### Get item with id (GET)

``` 
> GET /api/{collection}/{id}

200 OK          : Item is found
400 Bad Request : Collection is not found
404 Not Found   : Item is not found
```

Get user with `id` _1_.

```sh
$ curl http://localhost:57602/api/users/1
```

```json
{ "id": 1, "name": "Phil", "age": 40, "location": "NY" }
```

##### Get nested items

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

Example query will return address object from the employee with `id` _1_ from the company with `id` _0_.

```sh
$ curl http://localhost:57602/api/company/0/employees/1/address
```

```json
{ "address": { "city": "London" } }
```

#### Add item (POST)

```txt
> POST /api/{collection}

201 Created     : New item is created
400 Bad Request : New item is null
409 Conflict    : Collection is an object
```

Add _{ "name": "Phil", "age": 40, "location": "NY" }_ to users.

```sh
$ curl -H "Accept: application/json" -H "Content-type: application/json" -X POST -d '{ "name": "Phil", "age": 40, "location": "NY" }' http://localhost:57602/api/users/
```

Response has new item's id and a Location header that contains the path to the new item.

```txt
{ "id": 6 }

Headers:
Location=http://localhost:57602/api/users/6
```

If collection is empty and new item has an _id-field_ set, it will be used a first _id-value_. If _id-field_ is not set, _id-value_ will start from `0`.

#### Replace item (PUT)

```txt
> PUT /api/{collection}/{id}

204 No Content  : Item is replaced
400 Bad Request : Item is null
404 Not Found   : Item is not found
```

Replace user with `id` _1_ with object _{ "name": "Roger", "age": 28, "location": "SF" }_.

```sh
$ curl -H "Accept: application/json" -H "Content-type: application/json" -X PUT -d '{ "name": "Roger", "age": 28, "location": "SF" }' http://localhost:57602/api/users/1
```

#### Update item (PATCH)

Server supports [JSON patch](http://jsonpatch.com/) and [JSON merge patch](https://tools.ietf.org/html/rfc7396).

##### JSON Patch

```txt
> PATCH /api/{collection}/{id}

Content-type: application/json-patch+json

204 No Content             : Item updated
400 Bad Request            : PATCH is empty
404 Not Found              : Item is not found
415 Unsupported Media Type : Content type is not supported
```

Set `age` to _41_ and `work.rating` to _3.2_ from user with `id` _1_.

```sh
$ curl -H "Accept: application/json" -H "Content-type: application/json-patch+json" -X PATCH -d '[{ "op": "replace", "path": "age", "value": 41}, { "op": "replace", "path": "work/rating", "value": 3.2 }]' http://localhost:57602/api/users/1
```

```json
[
  { "op": "replace", "path": "age", "value": 41}, 
  { "op": "replace", "path": "work/rating", "value": 3.2 }
] 
```

##### JSON Merge Patch

```txt
> PATCH /api/{collection}/{id}

Content-type: application/json+merge-patch or application/merge-patch+json

204 No Content             : Item updated
400 Bad Request            : PATCH is empty
404 Not Found              : Item is not found
415 Unsupported Media Type : Content type is not supported
```


Set `age` to _41_ and `work.rating` to _3.2_ from user with `id` _1_.

```sh
$ curl -H "Accept: application/json" -H "Content-type: application/json+merge-patch" -X PATCH -d '{ "age": "41", "work": { "rating": 3.2 }}' http://localhost:57602/api/users/1
```

```json
{
  "age": 41,
  "work": {
    "rating": 3.2
  }
} 
```

_NOTE:_

> Due to the limitations of the merge patch, if the patch is anything other than an object, the result will always be to replace the entire target with the entire patch. Also, it is not possible to patch part of a target that is not an object, such as to replace just some of the values in an array.

https://tools.ietf.org/html/rfc7396#section-2

#### Delete item (DELETE)

``` 
> DELETE /api/{collection}/{id}

204 No Content  : Item deleted
404 Not Found   : Item is not found
```

Delete user with id _1_.

```sh
$ curl -X DELETE http://localhost:57602/api/users/1
```

#### OPTIONS method

OPTIONS method will return `Allow` header with a list of HTTP methods that may be used on the resource.

```sh
$ curl -X OPTIONS -v http://localhost:57602/api/
```

```txt
200 OK

Headers:
Allow: GET, POST, OPTIONS
```

#### HEAD method

HEAD method can be used to get the metadata and headers without receiving response body.

E.g. get user count without downloading large response body.

```sh
$ curl -X HEAD -v http://localhost:57602/api/users
```

```txt
200 OK

Headers:
X-Total-Count: 1249
```

### Async Operations

`/async` endpoint has long running operation for each update operation.

```txt
> POST/PUT/PATCH/DELETE /async/{collection}/{id}

202 Accepted    : New job started
400 Bad Request : Job not started

Headers:
Location=http://{url}:{port}/async/queue/{id}
```

Update operations will return location to job queue in headers.

Create new item. Curl has a verbose flag (`-v`). When it is used, curl will print response headers among other information.

```sh
$ curl -H "Accept: application/json" -H "Content-type: application/json" -X POST -d '{ "name": "Phil", "age": 40, "location": "NY" }' -v http://localhost:57602/async/users/
```

```txt
> GET /async/queue/{id}

200 OK        : Job running
303 See Other : Job ready
404 Not Found : Job not found

Headers:
Location=http://{url}:{port}/api/{collectionId}/{id}
```

When Job is ready, status code will be _redirect See Other_. Location header will have modified item's url.

After job is finished, it must be deleted manually

```txt
> DELETE /async/queue/{id}

204 No Content : Job deleted
404 Not Found  : Job not found
```

#### Job delay

Delay for operations can be set from `appsettings.json`. With long delay it is easier to simulate long running jobs.

```json
  "Jobs": {
    "DelayMs": 2000
  }
 ```

Delay value is milliseconds. Default value is 2000ms.

### GraphQL

GraphQL implementation is experimental and supports only basic queries and mutations. At the moment this is a good way to compare simple GraphQL and REST queries.
`/graphql` endpoint accepts requests with `application/graphql` or `application/json` content type. If the first, request body is GraphQL query string, whereas if the latter, request body is expected to be a valid JSON with parameter `query` containing the GraphQL query string.

```
> POST /graphql

Content-type: application/graphql
Body: [query/mutation]

OR

Content-type: application/json
Body: { "query": "[query/mutation]" }

200 OK              : Query/mutation successful 
400 Bad Request     : Query/mutation contains errors
501 Not Implemented : HTTP method and/or content-type combination not implemented
```

Alternatively, the `/graphql` endpoint also supports requests containing a valid GraphQL query as a `query` query parameter using either a GET or POST request. Queries in the JSON format are not supported as query parameters. Note that in the case of a POST request, the query supplied using the query parameter will take priority over any content in the request body, which will be ignored if the `query` query parameter is present.

```
> GET /graphql?query=[query/mutation]

OR

> POST /graphql?query=[query/mutation]

200 OK              : Query/mutation successful 
400 Bad Request     : Query/mutation contains errors
501 Not Implemented : HTTP method and/or content-type combination not implemented
```

Response is in JSON format. It contains `data` and `errors` fields. `errors` field is not present if there are no errors. 

```txt
{
  "data": { 
    "users": [ ... ],
    ...
  },
  "errors": [ ... ]
}
```` 

Implementation uses [graphql-dotnet](https://github.com/graphql-dotnet/graphql-dotnet) to parse Abstract Syntax Tree from the query.

#### Query

Query implementation supports equal filtering with arguments. Query's first field is the name of the collection.

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

Implementation accepts queries with operation type, with any query name (which is ignored) and query shorthands.

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
    children(age: 5){
      name
    }
  }
}
```

Example: get `familyName` and `age` of the `children` from `families` where `id` is 1 and `name`from all `users`.

```graphql
{
  families(id: 1) {
    familyName
    children {
      age
    }
  }
  users {
    name
  }
}
```

Execute query with curl:

```sh
$ curl -H "Content-type: application/graphql" -X POST -d "{ families(id: 1) { familyName children { age } } users { name } }" http://localhost:57602/graphql
```

Respose:

```json
{ 
  "data": {
    "families": [ 
      { 
        "familyName": "Day", 
        "children": [ 
          { "age": 14 }, 
          { "age": 18 }, 
          { "age": 9 } 
        ] 
      }
    ],
    "users": [ 
      { "name": "James" }, 
      { "name": "Phil" }, 
      { "name": "Raymond" }, 
      { "name": "Jarvis" } 
    ] 
  }
}
```

#### Mutation

Fake JSON Server supports dynamic mutations with the format defined below:

```graphql
mutation {
  [mutationName](input: {
    [optional id]
    [itemData/patch]
    }) {
      [collection]{
        [fields]
    }
}
```

Action is decided from the mutation name. Name follows pattern _add|update|replace|delete[collection]_ E.g. _deleteUsers_ will delete an item from the _users_ collection. Input object has an optional id-field and update data object. Return data is defined the same way as in queries.

##### Add item

`add{collection}`

Input contains object to be added with the collection's name.

```graphql
mutation {
  addUsers(input: {
    users: {
      name: James
      work: {
        name: ACME
      }
    }
  }) {
    users {
      id
      name 
    }
  }
}
```
Execute mutation with curl:

```sh
$ curl -H "Content-type: application/graphql" -X POST -d "mutation { addUsers(input: { users: { name: James work: { name: ACME } } }) { users { id name } } }" http://localhost:57602/graphql
```

Response:
```json
{ 
  "data": {
    "users":{
      "id": 12,
      "name": "James"
    }
  }
}
```

##### Update Item

`update{collection}`

```graphql
mutation {
  updateUsers(input: {
    id: 2
    patch:{
      name: Timothy
    }
  }) {
    users {
      id
      name 
    }
  }
}
```

Execute mutation with curl:

```sh
$ curl -H "Content-type: application/graphql" -X POST -d "mutation { updateUsers(input: { id: 2 patch:{ name: Timothy } }) { users { id name age }}}" http://localhost:57602/graphql
```

Response:
```json
{ 
  "data": { 
    "users": { 
      "id": 2, 
      "name": "Timothy", 
      "age": 25 
    } 
  } 
}
```

> NOTE: Update doesn't support updating child arrays

##### Replace item

`replace{collection}`

Input must contain id of the item to be replacecd and items full data in object named with collection's name.

```graphql
mutation {
  replaceUsers(input: {
    id: 5
    users:{
      name: Rick
      age: 44
      workplace: {
       companyName: ACME 
      }
    }
  }) {
    users {
      id
      name
      age
    }
  }
}
```

Execute mutation with curl:

```sh
$ curl -H "Content-type: application/graphql" -X POST -d "mutation { replaceUsers(input: { id: 1 users: { name: Rick age: 44 workplace: { name: ACME } } }) {users {id name age}}}" http://localhost:57602/graphql
```

Response:
```json
{
  "data": {
    "users": {
      "id": 1,
      "name": "Rick",
      "age": 44
    }
  }
}
```

##### Delete item

`delete{collection}`

Delete requires only the id of item to be deleted. Response will only contain success boolean `true/false`, so mutation doesn't need any definition for return data. 

```graphql
mutation {
  deleteUsers(input: {
    id: 4
  })
}
```

Execute mutation with curl:

```sh
$ curl -H "Content-type: application/graphql" -X POST -d "mutation { deleteUsers(input: { id: 4 }) }" http://localhost:57602/graphql
```

Response:

```json
{
  "data": {
    "Result": true
  }
}
```

## Guidelines

API follows best practices and recommendations from these guides:

* [REST CookBook](http://restcookbook.com/Resources/asynchroneous-operations/)
* [REST API Tutorial](http://www.restapitutorial.com/lessons/httpmethods.html)
* [Zalando Restful API Guidelines](https://zalando.github.io/restful-api-guidelines)
* [Microsoft API Design](https://docs.microsoft.com/en-us/azure/architecture/best-practices/api-design)
* [GitHub v3 Guide](https://developer.github.com/v3/guides/)
* [Introduction to GraphQL](http://graphql.org/learn/)
* [MDN Web Docs: ETag](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/ETag)
* [Designing GraphQL Mutations](https://dev-blog.apollodata.com/designing-graphql-mutations-e09de826ed97)
* [IETF Tools](https://tools.ietf.org/id/draft-snell-merge-patch-02.html#rfc.section.2)

## Other Links

* [Benchmark with wrk](docs/BenchmarkWrk.md)

## Releases

Releases are marked with Tag and can be found from [Releases](https://github.com/ttu/dotnet-fake-json-server/releases).

## Changelog

[Changelog](CHANGELOG.md)

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

## License

Licensed under the [MIT](LICENSE) License.

