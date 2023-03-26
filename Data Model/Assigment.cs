using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using CsvHelper;

namespace Data_Model;

public static class CsvSerializer
{
    public static IEnumerable<TType> Serialize<TType>(
        string       filePath,
        Encoding?    encoding = null,
        CultureInfo? culture  = null)
        where TType : new()
    {
        var reader = new StreamReader(filePath, encoding ?? Encoding.Default);

        using var csv = new CsvReader(reader, culture ?? CultureInfo.InvariantCulture);

        var records = csv.GetRecords<dynamic>().Cast<IDictionary<string, object>>();

        var type = typeof(TType);

        foreach (var record in records) {
            object? instance = Activator.CreateInstance(type);
            foreach (var property_info in type.GetProperties()) {
                if (property_info.PropertyType.IsArray) {
                    var      array_type = property_info.PropertyType.GetElementType()!;
                    string[] values     = ((string)record[property_info.Name]).Split(',');
                    var      converter  = TypeDescriptor.GetConverter(array_type);
                    var      array      = Array.CreateInstance(array_type, values.Length);

                    for (int i = 0; i < values.Length; i++) {
                        array.SetValue(converter.ConvertFrom(values[i]), i);
                    }

                    property_info.SetValue(instance, values);

                    yield return (TType)instance!;
                }
                else
                    switch (Type.GetTypeCode(property_info.PropertyType)) {
                    case >= TypeCode.SByte and <= TypeCode.UInt64:
                    {
                        var    converter = TypeDescriptor.GetConverter(property_info.PropertyType);
                        string str       = ((string)record[property_info.Name]).Replace(",", "");
                        object value     = converter.ConvertFrom(str is "" ? "0" : str) ?? 0;
                        property_info.SetValue(instance, value);
                        yield return (TType)instance!;
                        break;
                    }
                    case TypeCode.Single or TypeCode.Double or TypeCode.Decimal:
                    {
                        var    converter = TypeDescriptor.GetConverter(property_info.PropertyType);
                        string str       = ((string)record[property_info.Name]).Replace(',', '.');
                        object value     = converter.ConvertFrom(str) ?? 0;
                        property_info.SetValue(instance, value);
                        yield return (TType)instance!;
                        break;
                    }
                    case TypeCode.Boolean:
                    {
                        var    converter = TypeDescriptor.GetConverter(property_info.PropertyType);
                        object value     = converter.ConvertFrom(record[property_info.Name]) ?? false;
                        property_info.SetValue(instance, value);
                        yield return (TType)instance!;
                        break;
                    }
                    case TypeCode.String:
                    {
                        property_info.SetValue(instance, (string)record[property_info.Name]);
                        yield return (TType)instance!;
                        break;
                    }
                    default:
                        throw new NotSupportedException();
                    }
            }
        }
    }
}

public static class ObjectSerializer
{
    public static IEnumerable<TType> GetRecords<TType>(IEnumerable items)
        where TType : new()
    {
        HashSet<TType> set = new();

        var type = typeof(TType);
        foreach (object item in items) {
            TType instance = new();
            foreach (var property_info in item.GetType().GetProperties()) {
                object value = property_info.GetValue(item)!;

                var instance_prop = type.GetProperty(property_info.Name);
                if (instance_prop is null) continue;
                if (!instance_prop.PropertyType.IsArray && property_info.PropertyType.IsArray) {
                    foreach (object? val in (IEnumerable)value) {
                        instance_prop.SetValue(instance, val);
                    }

                    set.Add(instance);
                }
                else {
                    instance_prop.SetValue(instance, value);
                    set.Add(instance);
                }
            }
        }

        var group = set.GroupBy(obj =>
            type.GetProperties().First(propertyInfo => propertyInfo.Name.Contains("id")).GetValue(obj));

        return group.Select(x => x.First());
    }
}