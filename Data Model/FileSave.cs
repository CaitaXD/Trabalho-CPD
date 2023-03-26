using System.Reflection;
using System.Runtime.CompilerServices;
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
                object? property_value = property_info.GetValue(value);

                if (property_info.GetCustomAttribute<SerialFieldAttribute>() is { } serial) {
                    if (property_value is string str && str.Length < serial.Count) {
                        WriteObject(str.PadRight(serial.Count, (char)0), file);
                    }
                    else {
                        WriteObject(property_value, file);
                    }
                }
                else if (property_info.GetCustomAttribute<RangeFieldAttribute>() is { } index) {
                    using var index_file = File.Open(Path.Combine(directory, index.IndexFile), FileMode.Append);
                    WriteIndexedObject<Range>(property_value, file, index_file);
                }
            }
        }
    }

    public static int WriteObject(object? value, Stream fileStream)
    {
        var bytes = BinarySerializer.Serialize(value);
        fileStream.Write(bytes);
        return bytes.Length;
    }

    public static void WriteIndexedObject<TIndex>(object? value, Stream stream, Stream indexStream)
        where TIndex : unmanaged
    {
        var index_type = typeof(TIndex);

        if (index_type == typeof(Range)) {
            int offset      = (int)indexStream.Position;
            int written     = WriteObject(value, indexStream);
            var index_bytes = BinarySerializer.Serialize(offset..written);
            stream.Write(index_bytes);
        }
        else {
            long position    = indexStream.Position;
            var  offset      = Unsafe.As<long, TIndex>(ref position);
            var  index_bytes = BinarySerializer.Serialize(offset);
            stream.Write(index_bytes);
        }
    }
}