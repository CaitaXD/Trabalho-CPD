using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using CsvHelper;
using static System.Runtime.InteropServices.MemoryMarshal;

namespace Data_Model;

public static class CsvModule
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

        return set.GroupBy(x => x.GetType().GetProperties().First(x => x.Name.Contains("id")).GetValue(x))
            .Select(x => x.First());
    }

    public static IEnumerable<TType> ReadRecords<TType>(string directory)
        where TType : new()
    {
        var    type       = typeof(TType);
        var    file       = File.OpenRead(Path.Combine(directory, $"{type.Name}.bin"));
        var    properties = type.GetProperties();
        byte[] buffer     = new byte[1024];
        int    read       = 0;

        while (read < file.Length) {
            TType instance = new();
            foreach (var property_info in properties) {
                if (property_info.GetCustomAttribute<SerialFieldAttribute>() is { } serial) {
                    object value = ReadSerial(property_info, file, serial, ref read);
                    property_info.SetValue(instance, value);
                }
                else if (property_info.GetCustomAttribute<RangeFieldAttribute>() is { } index) {
                    object value = ReadIndexed(property_info, file, index, ref read);
                    property_info.SetValue(instance, value);
                }
            }

            yield return instance;
        }

        object ReadSerial(PropertyInfo propertyInfo, Stream stream, SerialFieldAttribute serial, ref int bytesRead)
        {
            var spn = buffer.AsSpan(serial.Offset, serial.Count);

            stream.ReadExactly(spn);

            bytesRead += spn.Length;

            return Type.GetTypeCode(propertyInfo.PropertyType) switch
            {
                TypeCode.String  => Encoding.UTF8.GetString(spn),
                TypeCode.Boolean => BitConverter.ToBoolean(spn),
                TypeCode.Byte    => buffer[0],
                TypeCode.SByte   => buffer[0],
                TypeCode.Int16   => BitConverter.ToInt16(spn),
                TypeCode.UInt16  => BitConverter.ToUInt16(spn),
                TypeCode.Int32   => BitConverter.ToInt32(spn),
                TypeCode.UInt32  => BitConverter.ToUInt32(spn),
                TypeCode.Int64   => BitConverter.ToInt64(spn),
                TypeCode.UInt64  => BitConverter.ToUInt64(spn),
                TypeCode.Single  => BitConverter.ToSingle(spn),
                TypeCode.Double  => BitConverter.ToDouble(spn),
                _                => throw new NotSupportedException()
            };
        }

        object ReadIndexed(PropertyInfo propertyInfo, Stream stream, RangeFieldAttribute index, ref int bytesRead)
        {
            var spn_offset = buffer.AsSpan(index.Offset, index.Count);
            stream.ReadExactly(spn_offset);
            bytesRead += spn_offset.Length;

            var range = Read<Range>(spn_offset);

            var index_file = File.OpenRead(Path.Combine(directory, index.IndexFile));

            index_file.Seek(range.Start.Value, SeekOrigin.Begin);

            if (buffer.Length < range.End.Value)
                buffer = new byte[range.End.Value];

            var spn_str = buffer.AsSpan(0, range.End.Value);

            index_file.ReadExactly(spn_str);

            return Type.GetTypeCode(propertyInfo.PropertyType) switch
            {
                TypeCode.String  => Encoding.UTF8.GetString(spn_str),
                TypeCode.Boolean => BitConverter.ToBoolean(spn_str),
                TypeCode.Byte    => spn_str[0],
                TypeCode.SByte   => spn_str[0],
                TypeCode.Int16   => BitConverter.ToInt16(spn_str),
                TypeCode.UInt16  => BitConverter.ToUInt16(spn_str),
                TypeCode.Int32   => BitConverter.ToInt32(spn_str),
                TypeCode.UInt32  => BitConverter.ToUInt32(spn_str),
                TypeCode.Int64   => BitConverter.ToInt64(spn_str),
                TypeCode.UInt64  => BitConverter.ToUInt64(spn_str),
                TypeCode.Single  => BitConverter.ToSingle(spn_str),
                TypeCode.Double  => BitConverter.ToDouble(spn_str),
                _                => throw new NotSupportedException()
            };
        }
    }


    public static int WriteObjects<TType>(IEnumerable<TType> values, string directory)
    {
        var       type = typeof(TType);
        using var file = File.Create(Path.Combine(directory, $"{type.Name}.bin"));

        var properties = type.GetProperties();

        foreach (var value in values.ToArray()) {
            foreach (var property_info in properties) {
                if (property_info.GetCustomAttribute<SerialFieldAttribute>() is { } serial) {
                    WriteSerial(property_info, file, serial, value);
                }
                else if (property_info.GetCustomAttribute<RangeFieldAttribute>() is { } index) {
                    WriteIndexed(property_info, file, index, value);
                }
            }
        }

        return properties.Length;

        void WriteSerial(PropertyInfo propertyInfo, Stream fileStream, SerialFieldAttribute field, object value)
        {
            object? property_value = propertyInfo.GetValue(value);

            switch (property_value) {
            case string str:
            {
                byte[] bytes = Encoding.Default.GetBytes(str);

                fileStream.Write(bytes);

                if (bytes.Length < field.Count) {
                    fileStream.Write(new byte[field.Count - bytes.Length]);
                }

                break;
            }
            case IEnumerable enumerable:
            {
                foreach (object item in enumerable) {
                    fileStream.Write(BitConverter.GetBytes((int)item));
                }

                break;
            }
            default:
                fileStream.Write(BitConverter.GetBytes((int)property_value!));
                break;
            }
        }

        void WriteIndexed(PropertyInfo propertyInfo, Stream stream, RangeFieldAttribute field, object value)
        {
            object? property_value = propertyInfo.GetValue(value);

            using var index_file = File.Open(Path.Combine(directory, field.IndexFile), FileMode.Append);

            int offset = (int)index_file.Position;

            switch (property_value) {
            case null:
            {
                stream.Write(new byte[field.Count]);
                break;
            }
            case string str:
            {
                byte[] bytes = Encoding.UTF8.GetBytes(str);

                index_file.Write(bytes);

                if (bytes.Length < field.Count) {
                    index_file.Write(new byte[field.Count - bytes.Length]);
                }

                var range = new Range(offset, bytes.Length);

                stream.Write(AsBytes(CreateReadOnlySpan(ref range, 1)));

                break;
            }
            case IEnumerable enumerable:
            {
                foreach (object item in enumerable) {
                    index_file.Write(BitConverter.GetBytes((int)item));
                }

                stream.Write(BitConverter.GetBytes(offset));

                break;
            }
            case { } when Type.GetTypeCode(propertyInfo.PropertyType) is >= TypeCode.Boolean and <= TypeCode.Double:
            {
                byte[] bytes = Type.GetTypeCode(propertyInfo.PropertyType) switch
                {
                    TypeCode.Boolean => BitConverter.GetBytes((bool)property_value!),
                    TypeCode.Byte    => new[] { (byte)property_value! },
                    TypeCode.SByte   => new[] { (byte)(sbyte)property_value! },
                    TypeCode.Int16   => BitConverter.GetBytes((short)property_value!),
                    TypeCode.UInt16  => BitConverter.GetBytes((ushort)property_value!),
                    TypeCode.Int32   => BitConverter.GetBytes((int)property_value!),
                    TypeCode.UInt32  => BitConverter.GetBytes((uint)property_value!),
                    TypeCode.Int64   => BitConverter.GetBytes((long)property_value!),
                    TypeCode.UInt64  => BitConverter.GetBytes((ulong)property_value!),
                    TypeCode.Single  => BitConverter.GetBytes((float)property_value!),
                    TypeCode.Double  => BitConverter.GetBytes((double)property_value!),
                    _                => throw new UnreachableException()
                };
                index_file.Write(bytes);
                stream.Write(BitConverter.GetBytes(offset));
                break;
            }
            }
        }
    }
}

public static partial class AssigmentConverter
{
}