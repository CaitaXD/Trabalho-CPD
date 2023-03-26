using System.Reflection;
using System.Runtime.InteropServices;

namespace Data_Model;

public static class FileSave
{
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
                    object? value = ReadSerial(property_info, file, serial, ref read, buffer);
                    property_info.SetValue(instance, value);
                }
                else if (property_info.GetCustomAttribute<RangeFieldAttribute>() is { } index) {
                    using var index_file = File.OpenRead(Path.Combine(directory, index.IndexFile));
                    object? value = ReadIndexed(property_info, file, index_file, ref read,
                        buffer.AsSpan(index.Offset, index.Count));
                    property_info.SetValue(instance, value);
                }
            }

            yield return instance;
        }
    }

    public static object? ReadSerial(PropertyInfo propertyInfo, Stream stream, SerialFieldAttribute serial,
        ref int                                   bytesRead,
        Span<byte>                                buffer)
    {
        var spn = buffer.Slice(serial.Offset, serial.Count);
        stream.ReadExactly(spn);
        bytesRead += spn.Length;
        return BinarySerializer.Deserialize(spn, propertyInfo.PropertyType);
    }

    public static object? ReadIndexed(PropertyInfo propertyInfo, Stream stream, Stream indexFile, ref int bytesRead,
        Span<byte>                                 indexBytes)
    {
        stream.ReadExactly(indexBytes);
        bytesRead += indexBytes.Length;
        var range = MemoryMarshal.Read<Range>(indexBytes);
        indexFile.Seek(range.Start.Value, SeekOrigin.Begin);

        byte[] bytes = new byte[range.End.Value];
        indexFile.ReadExactly(bytes);
        return BinarySerializer.Deserialize(bytes, propertyInfo.PropertyType);
    }

    public static void WriteObjects<TType>(IEnumerable<TType> values, string directory)
    {
        var       type = typeof(TType);
        using var file = File.Create(Path.Combine(directory, $"{type.Name}.bin"));

        var properties = type.GetProperties();

        foreach (var value in values.ToArray()) {
            foreach (var property_info in properties) {
                if (property_info.GetCustomAttribute<SerialFieldAttribute>() is { } serial) {
                    WritePropertyValue(property_info, file, value);
                }
                else if (property_info.GetCustomAttribute<RangeFieldAttribute>() is { } index) {
                    using var index_file = File.Open(Path.Combine(directory, index.IndexFile), FileMode.Append);
                    WriteIndexed(property_info, file, index_file, value);
                }
            }
        }
    }

    public static int WritePropertyValue(PropertyInfo propertyInfo, Stream fileStream, object? value)
    {
        object? property_value = propertyInfo.GetValue(value);
        byte[]  bytes          = BinarySerializer.Serialize(property_value);
        fileStream.Write(bytes);
        return bytes.Length;
    }

    public static void WriteIndexed(PropertyInfo propertyInfo, Stream stream, Stream indexFile, object? value)
    {
        int    offset      = (int)indexFile.Position;
        int    written     = WritePropertyValue(propertyInfo, indexFile, value);
        byte[] range_bytes = BinarySerializer.Serialize(offset..written);
        stream.Write(range_bytes);
    }
}