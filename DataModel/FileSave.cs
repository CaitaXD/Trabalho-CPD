using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using DataModel.DataStructures;
using DataModel.DataStructures.FileSystem;

namespace DataModel;

public static class FileSave
{
    
    static readonly HashSet<IDisposable> Files = new HashSet<IDisposable>();
    
    public static IEnumerable<TType> ReadObjects<TType>(string directory)
        where TType : new()
    {
        var    type       = typeof(TType);
        var    file       = GetOrOpenFile(Files, Path.Combine(directory, $"{type.Name}.bin"), FileMode.Open, out _);
        var    properties = type.GetProperties();
        byte[] buffer     = new byte[1024];


        while (file.Position < file.Length) {
            TType instance = new();
            foreach (var property_info in properties) {
                if (property_info.GetCustomAttribute<SerialFieldAttribute>() is { } serial_field_attribute) {
                    object? value = ReadSerial(property_info.PropertyType, file, serial_field_attribute, buffer);
                    property_info.SetValue(instance, value);
                }
                else if (property_info.GetCustomAttribute<RangeFieldAttribute>() is { } range_field_attribute) {
                    string  path        = Path.Combine(directory, $"{property_info.PropertyType.Name}.bin");
                    var     index_file  = GetOrOpenFile(Files, path, FileMode.Open, out _);
                    var     index_bytes = buffer.AsSpan(range_field_attribute.Offset, range_field_attribute.Count);
                    object? value       = ReadIndexed(property_info.PropertyType, file, index_file, index_bytes);
                    property_info.SetValue(instance, value);
                }
                else if (property_info.GetCustomAttribute<EntityFieldAttribute>() is { } entity_field_attribute) {
                    string path        = Path.Combine(directory, $"{property_info.PropertyType.Name}.bin");
                    var    entity_file = GetOrOpenFile(Files, path, FileMode.Open, out _);

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
                else if (property_info.GetCustomAttribute<PatriciaFieldAttribute>() is { } trie_field_attribute) {
                    object value = ReadPatricia(directory, file, property_info, trie_field_attribute);
                    property_info.SetValue(instance, value);
                }
            }

            yield return instance;
        }

        foreach (var disposable in Files) {
            disposable.Dispose();
        }

        Files.Clear();
    }

    static object ReadPatricia(
        string       directory,
        Stream       file,
        PropertyInfo propertyInfo,
        PatriciaFieldAttribute attribute)
    {
        var    binary_reader = new BinaryReader(file, Encoding.UTF8, true);
        string path          = Path.Combine(directory, $"{attribute.IndexFile}");
        var    file_mode     = File.Exists(path) ? FileMode.Open : FileMode.Create;

        var trie_file = GetOrOpenFile(Files, path, file_mode, out _);

        if (propertyInfo.PropertyType == typeof(string)) {
            var    patricia_stream = new PatriciaStream(trie_file);
            
            if (binary_reader.BaseStream.Position == binary_reader.BaseStream.Length) {
                return string.Empty;
            }
            file.Position += attribute.Offset;
            int    encoding        = binary_reader.ReadInt32();
            string value           = patricia_stream.Decode(encoding);
            return value;
        }
        else {
            throw new NotImplementedException();
        }
    }

    static object? ReadEntity<TIndex>(Type type, Stream entityIndexFile, Stream indexFile,
        EntityFieldAttribute               attribute,
        byte[]                             buffer,
        string                             directory)
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
                string path              = Path.Combine(directory, $"{range_field_attribute.IndexFile}");
                var    object_index_file = GetOrOpenFile(Files, path, FileMode.Open, out _);
                var    index_bytes       = buffer.AsSpan(range_field_attribute.Offset, range_field_attribute.Count);

                object? value = ReadIndexed(
                    property_info.PropertyType,
                    entityIndexFile,
                    object_index_file,
                    index_bytes);

                property_info.SetValue(instance, value);
            }
            else if (property_info.GetCustomAttribute<PatriciaFieldAttribute>() is { } patricia_field_attribute) {
                object value = ReadPatricia(directory, entityIndexFile, property_info, patricia_field_attribute);
                property_info.SetValue(instance, value);
            }
        }

        return instance;
    }

    static object? ReadSerial(Type type, Stream stream, SerialFieldAttribute serial,
        Span<byte>                 buffer)
    {
        var spn = buffer.Slice(serial.Offset, serial.Count);
        stream.ReadExactly(spn);
        return BinarySerializer.Deserialize(spn, type);
    }

    static object? ReadIndexed(Type type, Stream indexFile, Stream objectFile,
        Span<byte>                  indexBytes)
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

        string path = Path.Combine(directory, $"{type.Name}.bin");
        var    file = GetOrOpenFile(Files, path, fileMode, out _);

        var properties = type.GetProperties();

        foreach (var value in values.ToArray()) {
            WriteObjectProprieties(directory, properties, value, file);
        }
        foreach (var disposable in Files.OfType<PatriciaStream>()) {
            disposable.FLush();
        }
        
        foreach (var disposable in Files) {
            disposable.Dispose();
        }

        Files.Clear();
    }
    
    static void WriteObjectProprieties<TType>(string directory,
        IEnumerable<PropertyInfo>                    properties,
        TType                                        value,
        Stream                                       file)
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
                string path       = Path.Combine(directory, index.IndexFile);
                var    index_file = GetOrOpenFile(Files, path, FileMode.Append, out _);
                WriteIndexedObject<Range>(property_value, file, index_file);
            }
            else if (property_info.GetCustomAttribute<EntityFieldAttribute>() is { } entity_field_attribute) {
                string path        = Path.Combine(directory, $"{property_value!.GetType().Name}.bin");
                var    entity_file = GetOrOpenFile(Files, path, FileMode.Append, out _);

                var entity_type       = property_value!.GetType();
                var entity_properties = entity_type.GetProperties();
                int offset            = (int)entity_file.Position;
                file.Write(BinarySerializer.Serialize(offset));

                WriteObjectProprieties(directory, entity_properties, property_value!, entity_file);
            }
            else if (property_info.GetCustomAttribute<PatriciaFieldAttribute>() is { } patricia_index) {
                string         path          = Path.Combine(directory, $"{patricia_index.IndexFile}");
                var            patricia_file = GetOrOpenFile(Files, path, FileMode.Open, out bool created);
                PatriciaStream patricia_stream;

                if (created) {
                    patricia_stream = new PatriciaStream(patricia_file);
                    Files.Add(patricia_stream);
                }
                else {
                    patricia_stream = Files.OfType<PatriciaStream>().First();
                }

                var buffer = patricia_stream.Buffer;

                if (property_value is string str) {
                    
                   int encoding = patricia_stream.Add(str);

                    var index_bytes = BinarySerializer.Serialize(encoding);
                    file.Write(index_bytes);
                }
                else {
                    throw new NotImplementedException();
                }
            }
        }
    }

    static FileStream GetOrOpenFile(ISet<IDisposable> disposables, string path, FileMode fileMode,
        out bool                                      created)
    {
        var entity_file = disposables.OfType<FileStream>().FirstOrDefault(x => x.Name == path);

        if (entity_file != null) {
            created = false;
            return entity_file;
        }

        if (!File.Exists(path) && fileMode.HasFlag(FileMode.Open)) {
            fileMode = FileMode.OpenOrCreate;
        }

        entity_file = File.Open(path, fileMode);
        disposables.Add(entity_file);

        created = true;
        return entity_file;
    }

    static int WriteObject(object? value, Stream fileStream)
    {
        var bytes = BinarySerializer.Serialize(value);
        fileStream.Write(bytes);
        return bytes.Length;
    }

    static int WriteIndexedObject<TIndex>(object? value, Stream stream, Stream indexStream)
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