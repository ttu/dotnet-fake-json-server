using JsonFlatFileDataStore;

namespace FakeServer.Common
{
    public static class ExtensionMethods
    {
        public static bool IsCollection(this IDataStore ds, string id) => ds.GetKeys(JsonFlatFileDataStore.ValueType.Collection).ContainsKey(id);

        public static bool IsItem(this IDataStore ds, string id) => ds.GetKeys(JsonFlatFileDataStore.ValueType.Item).ContainsKey(id);
    }
}