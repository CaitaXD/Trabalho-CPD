using System.Runtime.InteropServices;

namespace Data_Model;

public static class BinarySerializer
{
    public static byte[] Serialize(object? obj)
    {
        return obj switch
        {
            null       => Array.Empty<byte>(),
            string str => System.Text.Encoding.UTF8.GetBytes(str),
            _          => SerializeBuiltIn(obj)
        };
    }

    public static object? Deserialize(Span<byte> span, Type type)
    {
        if (type == typeof(string)) {
            return System.Text.Encoding.UTF8.GetString(span);
        }
        else {
            return DeserializeBuiltIn(span, type);
        }
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