using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using DataModel.DataStructures.Generic;

namespace DataModel;

public static class FileSave
{
    public static IEnumerable<TType> ReadObjects<TType>(string directory)
        where TType : new()
    {
        var       type       = typeof(TType);
        using var file       = File.OpenRead(Path.Combine(directory, $"{type.Name}.bin"));
        var       properties = type.GetProperties();
        byte[]    buffer     = new byte[1024];


        while (file.Position < file.Length) {
            TType instance = new();
            foreach (var property_info in properties) {
                if (property_info.GetCustomAttribute<SerialFieldAttribute>() is { } serial_field_attribute) {
                    object? value = ReadSerial(property_info.PropertyType, file, serial_field_attribute, buffer);
                    property_info.SetValue(instance, value);
                }
                else if (property_info.GetCustomAttribute<RangeFieldAttribute>() is { } range_field_attribute) {
                    using var index_file  = File.OpenRead(Path.Combine(directory, range_field_attribute.IndexFile));
                    var       index_bytes = buffer.AsSpan(range_field_attribute.Offset, range_field_attribute.Count);
                    object?   value       = ReadIndexed(property_info.PropertyType, file, index_file, index_bytes);
                    property_info.SetValue(instance, value);
                }
                else if (property_info.GetCustomAttribute<EntityFieldAttribute>() is { } entity_field_attribute) {
                    using var entity_file =
                        File.OpenRead(Path.Combine(directory, $"{property_info.PropertyType.Name}.bin"));

                    object? value = ReadEntity<int>(
                        property_info.PropertyType,
                        entity_file,
                        file,
                        entity_field_attribute,
                        buffer,
                        directory);

                    if (value is null) {
                        yield break;
                    }

                    property_info.SetValue(instance, value);
                }
                else if (property_info.GetCustomAttribute<PtriciaIndexAttribute>() is { } trie_field_attribute) {
                    using var trie_file =
                        File.OpenRead(Path.Combine(directory, $"{property_info.PropertyType.Name}.bin"));

                    throw new NotImplementedException();
                }
            }

            yield return instance;
        }
    }

    private static object? ReadEntity<TIndex>(Type type, Stream entityIndexFile, Stream indexFile,
        EntityFieldAttribute                       attribute,
        byte[]                                     buffer,
        string                                     directory)
        where TIndex : unmanaged
    {
        var bytes_index = buffer.AsSpan(attribute.Offset, Unsafe.SizeOf<TIndex>());
        indexFile.ReadExactly(bytes_index);
        long index = (long)Convert.ChangeType(MemoryMarshal.Read<TIndex>(bytes_index), typeof(long));
        entityIndexFile.Seek(index, SeekOrigin.Begin);
        var entity_bytes = buffer.AsSpan(0, attribute.EnitySize);
        entityIndexFile.ReadExactly(entity_bytes);

        object? instance = Activator.CreateInstance(type);
        foreach (var property_info in type.GetProperties()) {
            if (property_info.GetCustomAttribute<SerialFieldAttribute>() is { } serial_field_attribute) {
                if (entityIndexFile.Position + serial_field_attribute.Count > entityIndexFile.Length) {
                    return null;
                }

                object? value = ReadSerial(
                    property_info.PropertyType,
                    entityIndexFile,
                    serial_field_attribute,
                    buffer);

                property_info.SetValue(instance, value);
            }
            else if (property_info.GetCustomAttribute<RangeFieldAttribute>() is { } range_field_attribute) {
                var object_index_file = File.OpenRead(Path.Combine(directory, range_field_attribute.IndexFile));
                var index_bytes       = buffer.AsSpan(range_field_attribute.Offset, range_field_attribute.Count);

                object? value = ReadIndexed(
                    property_info.PropertyType,
                    entityIndexFile,
                    object_index_file,
                    index_bytes);

                property_info.SetValue(instance, value);
            }
        }

        return instance;
    }

    private static object? ReadSerial(Type type, Stream stream, SerialFieldAttribute serial,
        Span<byte>                         buffer)
    {
        var spn = buffer.Slice(serial.Offset, serial.Count);
        stream.ReadExactly(spn);
        return BinarySerializer.Deserialize(spn, type);
    }

    private static object? ReadIndexed(Type type, Stream indexFile, Stream objectFile,
        Span<byte>                          indexBytes)
    {
        indexFile.ReadExactly(indexBytes);
        var range = MemoryMarshal.Read<Range>(indexBytes);
        objectFile.Seek(range.Start.Value, SeekOrigin.Begin);

        byte[] bytes = new byte[range.End.Value];
        objectFile.ReadExactly(bytes);
        return BinarySerializer.Deserialize(bytes, type);
    }

    public static void WriteObjects<TType>(IEnumerable<TType> values, string directory,
        FileMode                                              fileMode = FileMode.Append)
    {
        var type = typeof(TType);

        using var file = File.Open(Path.Combine(directory, $"{type.Name}.bin"), fileMode);

        var properties = type.GetProperties();

        foreach (var value in values.ToArray()) {
            WriteObjectProprieties(directory, properties, value, file);
        }
    }

    private static void WriteObjectProprieties<TType>(string directory, IEnumerable<PropertyInfo> properties,
        TType                                                value,
        Stream                                               file)
    {
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
            else if (property_info.GetCustomAttribute<EntityFieldAttribute>() is { } entity_field_attribute) {
                var       entity_type = property_value!.GetType();
                var       entity_properties = entity_type.GetProperties();
                using var entity_file = File.Open(Path.Combine(directory, $"{entity_type.Name}.bin"), FileMode.Append);
                int       offset = (int)entity_file.Position;
                file.Write(BinarySerializer.Serialize(offset));
                WriteObjectProprieties(directory, entity_properties, property_value, entity_file);
            }
            else if (property_info.GetCustomAttribute<PtriciaIndexAttribute>() is { } patricia_index) {
                using var patricia_index_file = File.Open(Path.Combine(directory, $"{patricia_index.IndexFile}.bin"),
                    FileMode.Append);
                
                throw new NotImplementedException();
            }
        }
    }

    private static int WriteObject(object? value, Stream fileStream)
    {
        var bytes = BinarySerializer.Serialize(value);
        fileStream.Write(bytes);
        return bytes.Length;
    }

    private static int WriteIndexedObject<TIndex>(object? value, Stream stream, Stream indexStream)
        where TIndex : unmanaged
    {
        var index_type = typeof(TIndex);

        if (index_type == typeof(Range)) {
            int offset      = (int)indexStream.Position;
            int written     = WriteObject(value, indexStream);
            var index_bytes = BinarySerializer.Serialize(offset..written);

            stream.Write(index_bytes);
            return written;
        }
        else {
            long offset      = indexStream.Position;
            int  written     = WriteObject(value, indexStream);
            var  index_bytes = BinarySerializer.Serialize(Unsafe.As<long, TIndex>(ref offset));
            stream.Write(index_bytes);
            return written;
        }
    }
}