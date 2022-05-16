# Changelog

### [Unreleased]
* FIXED: Basic Authentication unauthenticated result
* ADDED: API key authentication
* CHANGED: For empty collections return 200 status code instead of 404
* FIXED: Swagger authentication
* CHANGED: Target framework to .NET 5
* CHANGED: PATCH methods to require Content-Type: application/json+merge-patch or merge-patch+json
* ADDED: Content negotiation with Accept-header and support for CSV and XML
* ADDED: Client Credentials support to token authentication
* FIXED: Identifiers can be stored in various JSON formats
* FIXED: Empty collection won't break following datastore operations
* CHANGED: Target framework to .NET Core 3.1
* CHANGED: ETag implementation to use ResponseCaching and Marvin.Cache.Headers

### [0.10.0] - 2019-06-16
* ADDED: Token endpoint to support username/password in JSON content
* ADDED: Define stored data id-field name
* CHANGED: Rename Common property from appsettings.json to DataStore
* CHANGED: Remove authentication.json and add Authentication settings to appsettings.json
* ADDED: GraphQL endpoint to support operations in query parameter
* ADDED: Configurable response transfrom middleware

### [0.9.1] - 2019-02-08
* CHANGED: Target framework to .NET Core 2.2
* FIXED: urls command line parameter

### [0.9.0] - 2019-01-31
* ADDED: Support for paging with page and per_page
* ADDED: User defined location for the static files (including SPAs)
* ADDED: Console parameter for help and version
* FIXED: Adding to the empty collection will use the value of the id-field if it is set
* FIXED: Value parsing for DateTime and double to culture invariant
 
### [0.8.0] - 2018-10-14
* ADDED: Release as a dotnet tool
* CHANGED: Use executable location as a base path for settings files
* CHANGED: Add Serilog to appsettings and required package for console logging

### [0.7.0] - 2018-09-30
* JWT blacklist to check token identifier from JWT ID (jti)
* Add query support for sorting
* Fix for HEAD method use and ETag to support HEAD method
* .NET Core version to 2.1
* Support for objects
* Do not return empty arrays with GraphQL filter

### [0.6.0] - 2017-11-30
* Caching of unchanged resources with ETag and If-None-Match headers
* Avoiding mid-air collisions with ETag and If-Match headers
* Authorization header input to Swagger if authentication is enabled
* Token endpoint to Swagger if JWT authentication is used
* Blacklist tokens with logout endpoint

### [0.5.0] - 2017-10-20
* Support for HTTP HEAD Method
* Experimental GraphQL mutations support
 
### [0.4.0] - 2017-09-13
* Basic Authentication
* Select fields to return from query
* Upgrade to ASP.NET Core 2.0

### [0.3.0] - 2017-08-17
* Offset and limit slice option
* Option for query to return JSON object, instead of the results array
* Full-text search
* Experimental GraphQL query support

### [0.2.0] - 2017-07-31
* Get Controller's route template from constant value
* HTTP OPTIONS method
* Remove obsolete status endpoint
* Query filter operators
* Pagination headers

### [0.1.0] - 2017-07-14
Features in chronological order.
* GET, POST, PUT, PATCH, DELETE
* Static files and CORS
* Swagger
* Logging
* Token Authentication
* WebSocket
* Reload endpoint
* Docker
* Long running jobs
* Simulate delay and errors
* Upsert option for PUT
* Eager data reload option
* Id field can be integer or string
