using System.Runtime.InteropServices;

namespace Data_Model;

public static class Converter
{
    public static byte[] GetBytes(object obj)
    {
        int    size = Marshal.SizeOf(obj);
        byte[] arr  = new byte[size];

        nint ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(obj, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);
        Marshal.FreeHGlobal(ptr);

        return arr;
    }

    public static object? GetObject(byte[] arr, object? obj)
    {
        if (obj == null) return obj;

        int  size = Marshal.SizeOf(obj);
        nint ptr  = Marshal.AllocHGlobal(size);

        Marshal.Copy(arr, 0, ptr, size);

        obj = Marshal.PtrToStructure(ptr, obj.GetType());
        Marshal.FreeHGlobal(ptr);

        return obj;
    }
}