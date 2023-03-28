using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Data_Model;

public static class BinarySerializer
{
    public const BindingFlags Size          = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    public const BindingFlags Serialization = BindingFlags.Public | BindingFlags.Instance  | BindingFlags.Static;

    public static Span<byte> Serialize(object? obj,
        BindingFlags                           bindingFlags = Serialization)
    {
        var bytes = (List<byte>)SerializeObjectRecursive(obj, new List<byte>(), bindingFlags);
        return CollectionsMarshal.AsSpan(bytes);
    }

    public static object? Deserialize(Span<byte> bytes, Type type,
        BindingFlags                             bindingFlags = Serialization)
    {
        if (type == typeof(string)) {
            return Encoding.UTF8.GetString(bytes);
        }
        else if (type.IsArray) {
            return DeserializeArray(bytes, type);
        }
        else if (type.IsUnManaged()) {
            return DeserializeUnManaged(bytes, type);
        }
        else {
            return DeserializeObjectRecursive(bytes, type, bindingFlags);
        }
    }

    public static object? DeserializeObjectRecursive(Span<byte> bytes, Type type,
        BindingFlags                                            bindingFlags = Serialization)
    {
        if (type == typeof(string)) {
            return Encoding.UTF8.GetString(bytes);
        }
        else if (type.IsUnManaged()) {
            return DeserializeUnManaged(bytes, type);
        }

        object? instance = Activator.CreateInstance(type);

        var properties = type.GetProperties(bindingFlags);

        foreach (var property in properties) {
            var property_type = property.PropertyType;

            if (property_type.IsUnManaged()) {
                property.SetValue(instance, DeserializeUnManaged(bytes, property_type));
            }
            else if (property_type.IsArray) {
                property.SetValue(instance, DeserializeArray(bytes, property_type));
            }
            else {
                property.SetValue(instance, DeserializeObjectRecursive(bytes, property_type, bindingFlags));
            }
        }

        return instance;
    }

    public static object DeserializeArray(Span<byte> bytes, Type type)
    {
        var element_type = type.GetElementType()!;

        var array = Array.CreateInstance(element_type, bytes.Length / SizeOf(element_type));

        for (int i = 0; i < array.Length; i++) {
            array.SetValue(Deserialize(bytes[(i * SizeOf(element_type))..], element_type), i);
        }

        return array;
    }

    public static int SizeOf(Type type,
        BindingFlags              bindingFlags = Size)
    {
        if (type.TryMarshalSize(out int size)) {
            return size;
        }
        else {
            return type.GetFields(bindingFlags)
                .Sum(f => SizeOf(f.FieldType));
        }
    }

    public static bool TryMarshalSize(this Type type, out int sizeOf)
    {
        try {
            sizeOf = Marshal.SizeOf(type);
            return true;
        }
        catch (Exception) {
            sizeOf = default;
            return false;
        }
    }

    public static bool IsUnManaged(this Type type)
    {
        try {
            _ = Marshal.SizeOf(type);
            return true;
        }
        catch (Exception) {
            return false;
        }
    }

    // ReSharper disable once ReturnTypeCanBeEnumerable.Global
    public static IList<byte> SerializeObjectRecursive(object? obj, IList<byte>? accumulator = null,
        BindingFlags                                           bindingFlags = Serialization)
    {
        switch (obj) {
        case string str:
            accumulator ??= new List<byte>();
            accumulator.AddRange(Encoding.UTF8.GetBytes(str));
            return accumulator;
        case null:
            return accumulator ?? Array.Empty<byte>();
        }

        accumulator ??= new List<byte>();
        var type = obj.GetType();

        if (type.IsUnManaged()) {
            accumulator.AddRange(SerializerUnManaged(obj));
            return accumulator;
        }

        var properties = type.GetProperties(bindingFlags);
        foreach (var property in properties) {
            object? value         = property.GetValue(obj);
            var     property_type = property.PropertyType;
            if (value is null) {
                accumulator.Add(0);
            }
            else if (property_type.IsValueType) {
                accumulator.AddRange(SerializerUnManaged(value));
            }
            else if (property_type.IsArray) {
                SerializeArrayRecursive(value, accumulator);
            }
            else {
                SerializeObjectRecursive(value, accumulator, bindingFlags);
            }
        }

        return accumulator;
    }

    // ReSharper disable once ReturnTypeCanBeEnumerable.Global
    public static IList<byte> SerializeArrayRecursive(object obj, IList<byte>? accumulator = null)
    {
        accumulator ??= new List<byte>();

        var array        = (Array)obj;
        var element_type = obj.GetType().GetElementType()!;

        foreach (object item in array) {
            if (item is null) {
                accumulator.Add(0);
            }
            else if (element_type.IsValueType) {
                accumulator.AddRange(SerializerUnManaged(item));
            }
            else if (item.GetType().IsArray) {
                accumulator.AddRange(SerializeArrayRecursive(item, accumulator));
            }
            else {
                accumulator.AddRange(SerializeObjectRecursive(item, accumulator));
            }
        }

        return accumulator;
    }

    static byte[] SerializerUnManaged(object obj)
    {
        int    size = Marshal.SizeOf(obj);
        byte[] arr  = new byte[size];

        nint ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(obj, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);
        Marshal.FreeHGlobal(ptr);

        return arr;
    }

    static object? DeserializeUnManaged(Span<byte> arr, Type type)
    {
        int     size = Marshal.SizeOf(type);
        nint    ptr  = Marshal.AllocHGlobal(size);
        object? obj  = Marshal.PtrToStructure(ptr, type);

        Marshal.Copy(arr.ToArray(), 0, ptr, size);
        Marshal.FreeHGlobal(ptr);

        return obj;
    }
}

public static class ListExtensions
{
    public static void AddRange<T>(this IList<T> list, IEnumerable<T> items)
    {
        foreach (var item in items) {
            list.Add(item);
        }
    }
}