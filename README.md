.NET Fake JSON Server
--------------------------

REST API for json db

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

## Routes

For now supports only id as key field and integer as it's value type.

`api/{collection}/{id}`

List collections `GET api`
 
Get all users: `GET api/user`

Get user with id `GET api/user/1`

```json
{
    "id": 1,
    "name": "Phil",
    "age": 40,
    "city": "NY"
}
```

