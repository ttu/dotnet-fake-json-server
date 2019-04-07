using FakeServer.Common;
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
using System.Threading.Tasks;

namespace FakeServer.GraphQL
{
    // Functionaly mutilated from https://github.com/graphql-dotnet/graphql-dotnet/blob/master/src/GraphQL/Execution/DocumentExecuter.cs
    // This is real Here be dragons stuff, but it works, so it is enough for now

    public class GraphQLResult
    {
        public dynamic Data { get; set; }

        public List<string> Errors { get; set; }

        public List<dynamic> Notifications { get; set; }
    }

    public static class GraphQL
    {
        public static GraphQLResult HandleQuery(string query, IDataStore datastore)
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

            if (operation.OperationType == OperationType.Query)
            {
                try
                {
                    var fields = CollectFields(operation.SelectionSet);

                    var queryResult = ExecuteFields(datastore, null, fields, true);

                    return new GraphQLResult { Data = queryResult.ToDictionary(e => e.Key, e => e.Value.Data) };
                }
                catch (Exception e)
                {
                    return new GraphQLResult { Errors = new List<string> { e.Message } };
                }
            }
            else if (operation.OperationType == OperationType.Mutation)
            {
                try
                {
                    var fields = CollectFields(operation.SelectionSet);

                    var mutaationResults = ExecuteMutationFields(datastore, null, fields);

                    var responseData = mutaationResults.ToDictionary(pair => pair.Key, pair => pair.Value.Item1);
                    var notifications = mutaationResults.Select(pair => pair.Value.Item2).Where(e => e != null).ToList();

                    return new GraphQLResult { Data = responseData, Notifications = notifications };
                }
                catch (Exception e)
                {
                    return new GraphQLResult { Errors = new List<string> { e.Message } };
                }
            }

            return new GraphQLResult { Errors = new List<string> { $"{operation.OperationType} operation not supported" } };
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
                        fields[name] = Fields.Empty();
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

                    values.TryGetValue("if", out object ifObj);

                    return !(bool.TryParse(ifObj?.ToString() ?? string.Empty, out bool ifVal) && ifVal);
                }

                directive = directives.Find(DirectiveGraphType.Include.Name);
                if (directive != null)
                {
                    var values = GetArgumentValues(
                        DirectiveGraphType.Include.Arguments,
                        directive.Arguments,
                        null);

                    values.TryGetValue("if", out object ifObj);

                    return bool.TryParse(ifObj?.ToString() ?? string.Empty, out bool ifVal) && ifVal;
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

            if (input is VariableReference variable)
            {
                return variables?.ValueFor(variable.Name);
            }

            if (type is ListGraphType listType)
            {
                var listItemType = listType.ResolvedType;
                var list = input as ListValue;
                return list != null
                    ? list.Values.Select(item => CoerceValue(listItemType, item, variables)).ToArray()
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

        private static Dictionary<string, ExecutionResult> ExecuteFields(dynamic source, dynamic target, Dictionary<string, Fields> fields, bool isRoot = false)
        {
            return fields.ToDictionary<KeyValuePair<string, Fields>, string, ExecutionResult>(
                pair => pair.Key,
                pair => ResolveField(source, target, pair.Value, isRoot));
        }

        private static ExecutionResult ResolveField(dynamic source, dynamic target, Fields fields, bool isRoot = false)
        {
            if (target == null)
                target = new ExpandoObject();

            var resolveResult = new ExecutionResult
            {
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
                        var r = ResolveField(item, rootObject, i.Value);
                        rootObject = r.Data;
                    }
                    
                    if (rootObject != null)
                        result.Add(rootObject);
                }

                resolveResult.Data = result;
                return resolveResult;
            }
            else
            {
                var newSource = ((IDictionary<string, object>)source).ContainsKey(f.Name) ? ((IDictionary<string, object>)source)[f.Name] : null;

                var newTarget = target;

                if (newSource is ExpandoObject && subFields.Count > 0)
                {
                    if (f.Arguments.Any(a => GetValue(newSource, a.Name) != ((dynamic)a.Value).Value))
                        return resolveResult;

                    newTarget = new ExpandoObject();
                    ((IDictionary<string, object>)target)[f.Name] = newTarget;

                    foreach (var i in subFields)
                    {
                        var r = ResolveField(newSource, newTarget, i.Value);
                    }
                }
                else if (IsEnumerable(newSource?.GetType()) && subFields.Count > 0)
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
                            var r = ResolveField(item, rootObject, i.Value);
                            rootObject = r.Data;
                        }

                        if (rootObject != null)
                            newArray.Add(rootObject);
                    }

                    if (f.Arguments.Any() && newArray.Count == 0)
                        target = null;
                }
                else
                {
                    ((IDictionary<string, object>)target)[f.Name] = newSource;
                }

                resolveResult.Data = target;
                return resolveResult;
            }
        }

        private static Dictionary<string, dynamic> ExecuteMutationFields(dynamic source, dynamic target, Dictionary<string, Fields> fields, bool isRoot = false)
        {
            return fields.ToDictionary(
                pair => GetResponseName(pair),
                pair =>
                {
                    var allowedActions = new[] { "add", "update", "replace", "delete" };

                    var fieldsToUse = CollectFields(pair.Value.First().SelectionSet);
                    var collectionName = fieldsToUse.Any()
                                            ? fieldsToUse.FirstOrDefault().Key
                                            : allowedActions.Aggregate(pair.Value.First().Name, (e, c) => e.Replace(c, "")).ToLower();
                    var collection = ((IDataStore)source).GetCollection(collectionName);

                    if (pair.Value.First().Name.StartsWith("add"))
                    {
                        var newItem = ResolveMutationField(fieldsToUse.First().Key, target, pair.Value, isRoot);

                        var success = collection.InsertOne(newItem);
                        var itemId = ((dynamic)newItem).id;

                        dynamic updateData = success ? new { Method = "POST", Path = $"{collectionName}/{itemId}", Collection = collectionName, ItemId = itemId } : null;

                        ExecutionResult item = GetMutationReturnItem(source, fieldsToUse, itemId);
                        return Tuple.Create(item.Data as object, updateData);
                        //return item.Value as object;
                    }
                    else if (pair.Value.First().Name.StartsWith("update"))
                    {
                        dynamic id = GraphQL.GetInputId(pair);
                        ExpandoObject newItem = ResolveMutationField("patch", target, pair.Value, isRoot);

                        var success = collection.UpdateOne(id, newItem);

                        dynamic updateData = success ? new { Method = "PATCH", Path = $"{collectionName}/{id}", Collection = collectionName, ItemId = id } : null;

                        ExecutionResult item = GetMutationReturnItem(source, fieldsToUse, id);
                        return Tuple.Create(item.Data as object, updateData);
                        //return item.Value as object;
                    }
                    else if (pair.Value.First().Name.StartsWith("replace"))
                    {
                        dynamic id = GraphQL.GetInputId(pair);
                        dynamic newItem = ResolveMutationField(collectionName, target, pair.Value, isRoot);

                        // Make sure that new data has id field correctly
                        ObjectHelper.SetFieldValue(newItem, Config.IdField, id);
                        //newItem.id = id;

                        var success = collection.ReplaceOne(id, newItem as ExpandoObject);

                        dynamic updateData = success ? new { Method = "PUT", Path = $"{collectionName}/{id}", Collection = collectionName, ItemId = id } : null;

                        ExecutionResult item = GetMutationReturnItem(source, fieldsToUse, id);
                        return Tuple.Create(item.Data as object, updateData);
                        //return item.Value as object;
                    }
                    else if (pair.Value.First().Name.StartsWith("delete"))
                    {
                        dynamic id = GraphQL.GetInputId(pair);

                        var success = collection.DeleteOne(id);

                        dynamic updateData = success ? new { Method = "DELETE", Path = $"{collectionName}/{id}", Collection = collectionName, ItemId = id } : null;
                        var item = new ExecutionResult { Data = success };

                        return Tuple.Create(success, updateData);
                        //return success;
                    }
                    else
                    {
                        return Tuple.Create<dynamic, dynamic>(null, null);
                    }
                });
        }

        private static string GetResponseName(KeyValuePair<string, Fields> pair)
        {
            var fields = CollectFields(pair.Value.First().SelectionSet);
            return fields.Any() ? fields.FirstOrDefault().Key : "Result";
        }

        private static dynamic GetInputId(KeyValuePair<string, Fields> pair)
        {
            return ((dynamic)pair.Value.First()
                                        .Children.First(e => e is Arguments)
                                        .Children.First(e => ((dynamic)e).Name == "input")
                                        .Children.First()
                                        .Children.First(e => ((dynamic)e).Name == Config.IdField)
                                        .Children.First()).Value;
        }

        private static ExecutionResult GetMutationReturnItem(dynamic source, Dictionary<string, Fields> fieldsToUse, int id)
        {
            // Get all items and then select one with newly added id
            ExecutionResult value = ResolveField(source, null, fieldsToUse.First().Value, true);
            var items = value.Data as List<dynamic>;
            var item = new ExecutionResult() { Data = items.First(e => ObjectHelper.GetFieldValue(e, Config.IdField) == id) };
            return item;
            
        }

        private static ExpandoObject ResolveMutationField(string nameToProcess, dynamic newItem, Fields fields, bool isRoot = false)
        {
            if (newItem == null)
                newItem = new ExpandoObject();

            var resolveResult = new ExecutionResult
            {
            };

            var visitedFragments = new List<string>();

            var f = fields.First();

            foreach (var arg in f.Arguments)
            {
                if (arg.Name == "input")
                {
                    void HandleChilds(INode node, ExpandoObject exp, bool firstCall = false)
                    {
                        foreach (var child in node.Children)
                        {
                            var oField = child as ObjectField;

                            if (firstCall && oField.Name != nameToProcess)
                                continue;

                            var grandChild = child.Children;

                            if (grandChild.Count() == 1 && grandChild.First().Children.Count() == 0)
                            {
                                var valueOject = grandChild.First();
                                // Grandchild should be some IValue
                                if (valueOject is IValue)
                                {
                                    dynamic value;
                                    try
                                    {
                                        value = ((dynamic)valueOject).Value;
                                    }
                                    catch (Exception)
                                    {
                                        try
                                        {
                                            value = ((dynamic)valueOject).Name;
                                        }
                                        catch (Exception)
                                        {
                                            value = null;
                                        }
                                    }

                                    ((IDictionary<string, object>)exp)[oField.Name] = value;
                                }
                                else
                                {
                                    ((IDictionary<string, object>)exp)[oField.Name] = ((dynamic)grandChild.First()).Value;
                                }
                            }
                            else
                            {
                                if (!firstCall && oField != null)
                                {
                                    if (oField.Value is ListValue)
                                    {
                                        if (((IDictionary<string, object>)exp).ContainsKey(oField.Name) == false)
                                        {
                                            ((IDictionary<string, object>)exp)[oField.Name] = new List<ExpandoObject>();
                                        }

                                        foreach (var c in ((dynamic)oField.Value).Values)
                                        {
                                            var chidExp = new ExpandoObject();
                                            ((List<ExpandoObject>)((IDictionary<string, object>)exp)[oField.Name]).Add(chidExp);

                                            HandleChilds(c, chidExp);
                                        }
                                    }
                                    else
                                    {
                                        var chidExp = new ExpandoObject();
                                        ((IDictionary<string, object>)exp)[oField.Name] = chidExp;

                                        HandleChilds(child, chidExp);
                                    }
                                }
                                else
                                    HandleChilds(child, exp);
                            }
                        }
                    }

                    foreach (var child in arg.Children)
                    {
                        HandleChilds(child, newItem, true);
                    }
                }
            }

            return newItem;
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