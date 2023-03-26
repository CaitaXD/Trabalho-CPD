using System.Runtime.InteropServices;

namespace Data_Model;

public static class BinarySerializer
{
    public static Span<byte> Serialize(object? obj, bool serializeStatic = false)
    {
        return CollectionsMarshal.AsSpan((List<byte>)SerializeObjectRecursive(obj, null, serializeStatic));
    }

    public static object? Deserialize(Span<byte> span, Type type)
    {
        if (type == typeof(string)) {
            return System.Text.Encoding.UTF8.GetString(span);
        }
        else if (type.IsArray) {
            return DeserializeArray(span, type);
        }
        else {
            return DeserializeBuiltIn(span, type);
        }
    }

    public static object DeserializeArray(Span<byte> span, Type type)
    {
        if (type.GetElementType() is { IsValueType: true } element_type) {
            var array = Array.CreateInstance(element_type, span.Length / Marshal.SizeOf(element_type));

            for (int i = 0; i < array.Length; i++) {
                array.SetValue(DeserializeBuiltIn(span[(i * Marshal.SizeOf(element_type))..], element_type), i);
            }

            return array;
        }
        else {
            throw new NotSupportedException("Only value type arrays are currently supported.");
        }
    }

    private class U<T> where T : unmanaged
    {
    }

    public static bool IsUnManaged(this Type t)
    {
        try {
            _ = typeof(U<>).MakeGenericType(t);
            return true;
        }
        catch (Exception) {
            return false;
        }
    }

    // ReSharper disable once ReturnTypeCanBeEnumerable.Global
    public static IList<byte> SerializeObjectRecursive(object? obj, IList<byte>? accumulatedBytes = null,
        bool                                                   serializeStatic = false)
    {
        switch (obj) {
        case string str:
            accumulatedBytes ??= new List<byte>();
            accumulatedBytes.AddRange(System.Text.Encoding.UTF8.GetBytes(str));
            return accumulatedBytes;
        case null:
            return accumulatedBytes ?? Array.Empty<byte>();
        }

        accumulatedBytes ??= new List<byte>();
        var type = obj.GetType();

        if (type.IsUnManaged()) {
            accumulatedBytes.AddRange(SerializeBuiltIn(obj));
            return accumulatedBytes;
        }

        var properties = type.GetProperties();
        foreach (var property in properties) {
            if (serializeStatic == false && property.GetMethod?.IsStatic == true) {
                continue;
            }

            object? value         = property.GetValue(obj);
            var     property_type = property.PropertyType;
            if (value is null) {
                accumulatedBytes.Add(0);
            }
            else if (property_type.IsValueType) {
                accumulatedBytes.AddRange(SerializeBuiltIn(value));
            }
            else if (property_type.IsArray) {
                SerializeArrayRecursive(value, accumulatedBytes);
            }
            else {
                SerializeObjectRecursive(value, accumulatedBytes, serializeStatic);
            }
        }

        return accumulatedBytes;
    }

    // ReSharper disable once ReturnTypeCanBeEnumerable.Global
    public static IList<byte> SerializeArrayRecursive(object obj, IList<byte>? accumulatedBytes = null)
    {
        accumulatedBytes ??= new List<byte>();

        var array        = (Array)obj;
        var element_type = obj.GetType().GetElementType()!;

        foreach (object item in array) {
            if (item is null) {
                accumulatedBytes.Add(0);
            }
            else if (element_type.IsValueType) {
                accumulatedBytes.AddRange(SerializeBuiltIn(item));
            }
            else if (item.GetType().IsArray) {
                accumulatedBytes.AddRange(SerializeArrayRecursive(item, accumulatedBytes));
            }
            else {
                accumulatedBytes.AddRange(SerializeObjectRecursive(item, accumulatedBytes));
            }
        }

        return accumulatedBytes;
    }

    static byte[] SerializeBuiltIn(object obj)
    {
        int    size = Marshal.SizeOf(obj);
        byte[] arr  = new byte[size];

        nint ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(obj, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);
        Marshal.FreeHGlobal(ptr);

        return arr;
    }

    static object? DeserializeBuiltIn(Span<byte> arr, Type type)
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