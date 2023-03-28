using System.Collections;
using System.Reflection;

namespace DataModel;

public static class ObjectSerializer
{
    public static IEnumerable<TType> GetEntities<TType>(IEnumerable records)
        where TType : new() =>
        GetEntities(typeof(TType), records).OfType<TType>();
    
    private static IEnumerable<object> GetEntities(Type type, IEnumerable records)
    {
        HashSet<object> set        = new();
        var             properties = type.GetProperties();

        foreach (object record in records) {
            object instance = Activator.CreateInstance(type)!;
            foreach (var property_info in properties) {
                if (property_info.GetCustomAttribute<EntityFieldAttribute>() is { }) {
                    object prop_instance = Activator.CreateInstance(property_info.PropertyType)!;
                    AddProprietyRecursive(prop_instance, record);
                    property_info.SetValue(instance, prop_instance);
                }
                else {
                    AddProprietyRecursive(instance, record);
                }
            }

            set.Add(instance);
        }

        return set;
    }
    private static void AddProprietyRecursive(object instance, object proprietaryValue,
        HashSet<object>?                            processedObjects = null)
    {
        var type      = instance.GetType();
        var item_type = proprietaryValue.GetType();

        processedObjects ??= new HashSet<object>();

        foreach (var type_prop in type.GetProperties()) {
            if (type_prop.GetCustomAttribute<EntityFieldAttribute>() is { }) {
                object? prop_instance = type_prop.GetValue(instance);

                if (prop_instance is null) {
                    prop_instance = Activator.CreateInstance(type_prop.PropertyType)!;
                    type_prop.SetValue(instance, prop_instance);
                }

                if (processedObjects.Contains(instance)) {
                    continue;
                }

                processedObjects.Add(instance);

                AddProprietyRecursive(prop_instance, proprietaryValue, processedObjects);
            }
        }

        foreach (var property_info in item_type.GetProperties()) {
            var instance_prop = type.GetProperty(property_info.Name);
            if (instance_prop is null) continue;

            object value = property_info.GetValue(proprietaryValue)!;

            if (!instance_prop.PropertyType.IsArray && property_info.PropertyType.IsArray) {
                foreach (object? val in (IEnumerable)value) {
                    instance_prop.SetValue(instance, val);
                }
            }
            else {
                instance_prop.SetValue(instance, value);
            }
        }
    }
    private static IEnumerable<object> GetRecords(Type type, object item)
    {
        object instance  = Activator.CreateInstance(type)!;
        var    item_type = item.GetType();

        foreach (var property_info in item_type.GetProperties()) {
            var instance_prop = type.GetProperty(property_info.Name);
            if (instance_prop is null) continue;

            object value = property_info.GetValue(item)!;

            if (!instance_prop.PropertyType.IsArray && property_info.PropertyType.IsArray) {
                foreach (object? val in (IEnumerable)value) {
                    instance_prop.SetValue(instance, val);
                }

                yield return instance;
            }
            else {
                instance_prop.SetValue(instance, value);
                yield return instance;
            }
        }
    }
    private static IEnumerable<object> GetRecords(Type type, IEnumerable<object> items)
    {
        HashSet<object> set = new();

        foreach (object item in items) {
            set.UnionWith(GetRecords(type, item));
        }
        return set;
    }
}