using JsonFlatFileDataStore;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FakeServer.Test
{
    public class GraphQLTests
    {
        [Fact]
        public void Query_Families()
        {
            var filePath = UTHelpers.Up();

            var ds = new DataStore(filePath);

            var q = @"
                    query {
                          families {
                            familyName
                            children(age: 4) {
                              name
                              age
                            }
                          }
                    }";

            var results = GraphQL.GraphQL.HandleQuery(q, ds);

            var js = JsonConvert.SerializeObject(results);

            Assert.NotEmpty(js);

            Assert.Equal(4, results.Data["families"].Count);

            UTHelpers.Down(filePath);
        }

        [Theory]
        [InlineData("query { users { name }}")]
        [InlineData("query someName { users { name }}")]
        [InlineData("{ users { name }}")]
        public void Query_Users(string q)
        {
            var filePath = UTHelpers.Up();

            var ds = new DataStore(filePath);
            var results = GraphQL.GraphQL.HandleQuery(q, ds);

            Assert.Equal(4, results.Data["users"].Count);

            UTHelpers.Down(filePath);
        }

        [Fact]
        public void Query_Errors()
        {
            var filePath = UTHelpers.Up();

            var ds = new DataStore(filePath);

            var q = @" {  }";

            var results = GraphQL.GraphQL.HandleQuery(q, ds);

            Assert.Single(results.Errors);

            UTHelpers.Down(filePath);
        }

        [Fact]
        public void Mutation_Add_User()
        {
            var filePath = UTHelpers.Up();

            var ds = new DataStore(filePath);

            var muation = @"
                    mutation {
                          addUsers ( input: {
                            users: {
                              name: Jimmy
                              age: 22
                            }
                          }) {
                            users {
                              id
                              name
                              age
                            }
                          }
                    }";

            var results = GraphQL.GraphQL.HandleQuery(muation, ds);

            var id = ((dynamic)results.Data["users"]).id;
            var item = ds.GetCollection("users").AsQueryable().First(e => e.id == id);

            Assert.Equal("Jimmy", item.name);
            Assert.Equal(22, item.age);

            UTHelpers.Down(filePath);
        }

        [Fact]
        public void Mutation_Add_User_With_Child()
        {
            var filePath = UTHelpers.Up();

            var ds = new DataStore(filePath);

            var muation = @"
                    mutation {
                          addUsers ( input: {
                            users: {
                              name: Newton
                              age: 45
                              work: {
                                name: ACME
                              }
                            }
                          }) {
                            users {
                              id
                              name
                              age
                              work {
                                name
                              }
                            }
                          }
                    }";

            var results = GraphQL.GraphQL.HandleQuery(muation, ds);

            var id = ((dynamic)results.Data["users"]).id;
            var item = ds.GetCollection("users").AsQueryable().First(e => e.id == id);

            Assert.Equal("Newton", item.name);
            Assert.Equal(45, item.age);
            Assert.Equal("ACME", item.work.name);

            UTHelpers.Down(filePath);
        }

        [Fact]
        public void Mutation_Add_User_With_ChildList()
        {
            var filePath = UTHelpers.Up();

            var ds = new DataStore(filePath);

            var muation = @"
                    mutation {
                          addFamilies ( input: {
                            families: {
                              familyName: Newtons
                              children: [
                                {
                                  name: Mary
                                  age: 12
                                },
                                {
                                  name: James
                                  age: 16
                                }
                              ]
                            }
                          }) {
                            families {
                              id
                              familyName
                              notFoundProperty
                              children {
                                name
                                age
                              }
                            }
                          }
                    }";

            var results = GraphQL.GraphQL.HandleQuery(muation, ds);

            var id = ((dynamic)results.Data["families"]).id;
            var item = ds.GetCollection("families").AsQueryable().First(e => e.id == id);

            Assert.Equal("Newtons", item.familyName);
            Assert.Equal("Mary", item.children[0].name);
            Assert.Equal("James", item.children[1].name);
            Assert.Equal(16, item.children[1].age);

            UTHelpers.Down(filePath);
        }

        [Fact]
        public void Mutation_Update_User()
        {
            var filePath = UTHelpers.Up();

            var ds = new DataStore(filePath);

            dynamic original = ds.GetCollection("users").AsQueryable().First(e => e.id == 2);

            var muation = @"
                    mutation {
                          updateUsers ( input: {
                            id: 2
                            patch: {
                              name: Jimmy
                              age: 87
                            }
                          }) {
                            users {
                              id
                            }
                          }
                    }";

            var results = GraphQL.GraphQL.HandleQuery(muation, ds);

            var id = ((dynamic)results.Data["users"]).id;
            Assert.Equal(2, id);

            var item = ds.GetCollection("users").AsQueryable().First(e => e.id == id);

            Assert.Equal("Phil", original.name);
            Assert.Equal(25, original.age);
            Assert.Equal("London", original.location);

            Assert.Equal("Jimmy", item.name);
            Assert.Equal(87, item.age);
            Assert.Equal("London", item.location);

            UTHelpers.Down(filePath);
        }

        [Fact]
        public void Mutation_Replace_User_With_Child()
        {
            var filePath = UTHelpers.Up();

            var ds = new DataStore(filePath);

            var original = ds.GetCollection("users").AsQueryable().First(e => e.id == 2);
            Assert.NotNull(original);
            Assert.Equal("London", original.location);

            var muation = @"
                    mutation {
                          replaceUsers ( input: {
                            id: 2
                            users: {
                              name: Newton
                              age: 45
                              work: {
                                name: ACME
                              }
                            }
                          }) {
                            users {
                              id
                              name
                              age
                              work {
                                name
                              }
                            }
                          }
                    }";

            var results = GraphQL.GraphQL.HandleQuery(muation, ds);

            var item = ds.GetCollection("users").AsQueryable().First(e => e.id == 2);

            Assert.Equal("Newton", item.name);
            Assert.Equal(45, item.age);
            Assert.Equal("ACME", item.work.name);

            var itemDict = item as IDictionary<string, object>;
            Assert.False(itemDict.ContainsKey("location"));

            UTHelpers.Down(filePath);
        }

        [Fact]
        public void Mutation_Delete_User()
        {
            var filePath = UTHelpers.Up();

            var ds = new DataStore(filePath);

            dynamic original = ds.GetCollection("users").AsQueryable().First(e => e.id == 2);
            Assert.NotNull(original);

            var muation = @"
                    mutation {
                          deleteUsers ( input: {
                            id: 2
                          })
                    }";

            var results = GraphQL.GraphQL.HandleQuery(muation, ds);

            var success = results.Data["Result"];
            Assert.True(success);

            var item = ds.GetCollection("users").AsQueryable().FirstOrDefault(e => e.id == 2);
            Assert.Null(item);

            UTHelpers.Down(filePath);
        }
    }
}