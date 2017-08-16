using GraphQL;
using GraphQL.Execution;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQLParser.Exceptions;
using JsonFlatFileDataStore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace FakeServer.GraphQL
{
    // Functionaly mutilated from https://github.com/graphql-dotnet/graphql-dotnet/blob/master/src/GraphQL/Execution/DocumentExecuter.cs

    public class GraphQLResult
    {
        public dynamic Data { get; set; }

        public List<string> Errors { get; set; }
    }

    public static class GraphQL
    {
        public static async Task<GraphQLResult> HandleQuery(string query, IDataStore datastore)
        {
            Document d;

            try
            {
                IDocumentBuilder doc = new GraphQLDocumentBuilder();
                d = doc.Build(query);
            }
            catch (GraphQLSyntaxErrorException e)
            {
                return new GraphQLResult { Errors = new List<string> { e.Message } };
            }

            var operation = GetOperation("", d);

            if (operation.OperationType != OperationType.Query)
                return new GraphQLResult { Errors = new List<string> { $"{operation.OperationType} operation not supported" } };

            var fields = CollectFields(operation.SelectionSet);

            var data = await ExecuteFieldsAsync(datastore, null, fields, true);

            return new GraphQLResult { Data = data };
        }

        private static Operation GetOperation(string operationName, Document document)
        {
            var operation = !string.IsNullOrWhiteSpace(operationName)
                ? document.Operations.WithName(operationName)
                : document.Operations.FirstOrDefault();

            return operation;
        }

        private static Dictionary<string, Fields> CollectFields(
           SelectionSet selectionSet,
           Dictionary<string, Fields> fields = null,
           List<string> visitedFragmentNames = null)
        {
            if (fields == null)
            {
                fields = new Dictionary<string, Fields>();
            }

            selectionSet.Selections.Apply(selection =>
            {
                if (selection is Field field)
                {
                    if (!ShouldIncludeNode(field.Directives))
                    {
                        return;
                    }

                    var name = field.Alias ?? field.Name;
                    if (!fields.ContainsKey(name))
                    {
                        fields[name] = new Fields();
                    }
                    fields[name].Add(field);
                }
                else if (selection is FragmentSpread)
                {
                    // TODO
                }
                else if (selection is InlineFragment)
                {
                    // TODO
                }
            });

            return fields;
        }

        private static bool ShouldIncludeNode(Directives directives)
        {
            if (directives != null)
            {
                var directive = directives.Find(DirectiveGraphType.Skip.Name);
                if (directive != null)
                {
                    var values = GetArgumentValues(
                        DirectiveGraphType.Skip.Arguments,
                        directive.Arguments,
                        null);

                    object ifObj;
                    values.TryGetValue("if", out ifObj);

                    bool ifVal;
                    return !(bool.TryParse(ifObj?.ToString() ?? string.Empty, out ifVal) && ifVal);
                }

                directive = directives.Find(DirectiveGraphType.Include.Name);
                if (directive != null)
                {
                    var values = GetArgumentValues(
                        DirectiveGraphType.Include.Arguments,
                        directive.Arguments,
                        null);

                    object ifObj;
                    values.TryGetValue("if", out ifObj);

                    bool ifVal;
                    return bool.TryParse(ifObj?.ToString() ?? string.Empty, out ifVal) && ifVal;
                }
            }

            return true;
        }

        private static Dictionary<string, object> GetArgumentValues(QueryArguments definitionArguments, Arguments astArguments, Variables variables)
        {
            if (definitionArguments == null || !definitionArguments.Any())
            {
                return null;
            }

            return definitionArguments.Aggregate(new Dictionary<string, object>(), (acc, arg) =>
            {
                var value = astArguments?.ValueFor(arg.Name);
                var type = arg.ResolvedType;

                var coercedValue = CoerceValue(type, value, variables);
                coercedValue = coercedValue ?? arg.DefaultValue;
                acc[arg.Name] = coercedValue;

                return acc;
            });
        }

        private static object CoerceValue(IGraphType type, IValue input, Variables variables = null)
        {
            if (type is NonNullGraphType nonNull)
            {
                return CoerceValue(nonNull.ResolvedType, input, variables);
            }

            if (input == null)
            {
                return null;
            }

            var variable = input as VariableReference;
            if (variable != null)
            {
                return variables != null
                    ? variables.ValueFor(variable.Name)
                    : null;
            }

            if (type is ListGraphType listType)
            {
                var listItemType = listType.ResolvedType;
                var list = input as ListValue;
                return list != null
                    ? list.Values.Map(item => CoerceValue(listItemType, item, variables)).ToArray()
                    : new[] { CoerceValue(listItemType, input, variables) };
            }

            if (type is IObjectGraphType || type is InputObjectGraphType)
            {
                var complexType = type as IComplexGraphType;
                var obj = new Dictionary<string, object>();

                var objectValue = input as ObjectValue;
                if (objectValue == null)
                {
                    return null;
                }

                complexType.Fields.Apply(field =>
                {
                    var objectField = objectValue.Field(field.Name);
                    if (objectField != null)
                    {
                        var fieldValue = CoerceValue(field.ResolvedType, objectField.Value, variables);
                        fieldValue = fieldValue ?? field.DefaultValue;

                        obj[field.Name] = fieldValue;
                    }
                });

                return obj;
            }

            if (type is ScalarGraphType scalarType)
            {
                return scalarType.ParseLiteral(input);
            }

            return null;
        }

        private static Task<Dictionary<string, object>> ExecuteFieldsAsync(dynamic source, dynamic target, Dictionary<string, Fields> fields, bool isRoot = false)
        {
            return fields.ToDictionaryAsync<KeyValuePair<string, Fields>, string, ResolveFieldResult<object>, object>(
                pair => pair.Key,
                pair => ResolveFieldAsync(source, target, pair.Value, isRoot));
        }

        private static async Task<ResolveFieldResult<object>> ResolveFieldAsync(dynamic source, dynamic target, Fields fields, bool isRoot = false)
        {
            if (target == null)
                target = new ExpandoObject();

            var resolveResult = new ResolveFieldResult<object>
            {
                Skip = false
            };

            var subFields = new Dictionary<string, Fields>();
            var visitedFragments = new List<string>();

            var f = fields.First();

            subFields = CollectFields(f.SelectionSet, subFields, visitedFragments);

            dynamic result;

            if (isRoot)
            {
                result = new List<object>();
                var collection = ((IDataStore)source).GetCollection(f.Name);

                foreach (var item in collection.AsQueryable())
                {
                    if (f.Arguments.Any(a => GetValue(item, a.Name) != ((dynamic)a.Value).Value))
                        continue;

                    dynamic rootObject = new ExpandoObject();

                    foreach (var i in subFields)
                    {
                        var r = await ResolveFieldAsync(item, rootObject, i.Value).ConfigureAwait(false);
                        rootObject = r.Value;
                    }

                    if (rootObject != null)
                        result.Add(rootObject);
                }

                resolveResult.Value = result;
                return resolveResult;
            }
            else
            {
                var newSource = ((IDictionary<string, object>)source)[f.Name];

                var newTarget = target;

                if (newSource is ExpandoObject && subFields.Count > 0)
                {
                    if (f.Arguments.Any(a => GetValue(newSource, a.Name) != ((dynamic)a.Value).Value))
                        return resolveResult;

                    newTarget = new ExpandoObject();
                    ((IDictionary<string, object>)target)[f.Name] = newTarget;

                    foreach (var i in subFields)
                    {
                        var r = await ResolveFieldAsync(newSource, newTarget, i.Value).ConfigureAwait(false);
                    }
                }
                else if (IsEnumerable(newSource.GetType()) && subFields.Count > 0)
                {
                    dynamic newArray = Activator.CreateInstance(newSource.GetType());
                    ((IDictionary<string, object>)target)[f.Name] = newArray;

                    foreach (var item in newSource as IEnumerable<dynamic>)
                    {
                        if (f.Arguments.Any(a => GetValue(item, a.Name) != ((dynamic)a.Value).Value))
                            continue;

                        dynamic rootObject = new ExpandoObject();

                        foreach (var i in subFields)
                        {
                            var r = await ResolveFieldAsync(item, rootObject, i.Value).ConfigureAwait(false);
                            rootObject = r.Value;
                        }

                        if (rootObject != null)
                            newArray.Add(rootObject);
                    }
                }
                else
                {
                    ((IDictionary<string, object>)target)[f.Name] = newSource;
                }

                resolveResult.Value = target;
                return resolveResult;
            }
        }

        private static dynamic GetValue(dynamic item, string name)
        {
            if (item is ExpandoObject)
                return ((IDictionary<string, object>)item)[name];
            else
                return item.GetProperyValue(name);
        }

        private static bool IsEnumerable(Type toTest)
        {
            return typeof(IEnumerable).IsAssignableFrom(toTest) && toTest != typeof(string);
        }
    }
}