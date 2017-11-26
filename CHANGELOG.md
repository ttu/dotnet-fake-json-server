# Changelog

### [Unreleased]
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
