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

namespace DataModel;

public static class CsvSerializer
{
    public static IEnumerable<TType> Serialize<TType>(
        string       filePath,
        Encoding?    encoding = null,
        CultureInfo? culture  = null)
        where TType : new()
    {
        var set = new HashSet<TType>();

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

                    set.Add((TType)instance!);
                }
                else
                    switch (Type.GetTypeCode(property_info.PropertyType)) {
                    case >= TypeCode.SByte and <= TypeCode.UInt64:
                    {
                        var    converter = TypeDescriptor.GetConverter(property_info.PropertyType);
                        string str       = ((string)record[property_info.Name]).Replace(",", "");
                        object value     = converter.ConvertFrom(str is "" ? "0" : str) ?? 0;
                        property_info.SetValue(instance, value);
                        set.Add((TType)instance!);
                        break;
                    }
                    case TypeCode.Single or TypeCode.Double or TypeCode.Decimal:
                    {
                        var    converter = TypeDescriptor.GetConverter(property_info.PropertyType);
                        string str       = ((string)record[property_info.Name]).Replace(',', '.');
                        object value     = converter.ConvertFrom(str) ?? 0;
                        property_info.SetValue(instance, value);
                        set.Add((TType)instance!);
                        break;
                    }
                    case TypeCode.Boolean:
                    {
                        var    converter = TypeDescriptor.GetConverter(property_info.PropertyType);
                        object value     = converter.ConvertFrom(record[property_info.Name]) ?? false;
                        property_info.SetValue(instance, value);
                        set.Add((TType)instance!);
                        break;
                    }
                    case TypeCode.String:
                    {
                        property_info.SetValue(instance, (string)record[property_info.Name]);
                        set.Add((TType)instance!);
                        break;
                    }
                    default:
                        throw new NotSupportedException();
                    }
            }
        }

        return set;
    }
}