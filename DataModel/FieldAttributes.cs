using System.Buffers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static System.Runtime.InteropServices.MemoryMarshal;

namespace DataModel;

public class SerialFieldAttribute : Attribute
{
    public int Offset { get; init; }
    public int Count  { get; init; }
}

public class RangeFieldAttribute : Attribute
{
    public string IndexFile { get; init; }
    public int    Offset    { get; init; }
    public int    Count     { get; init; }

    public RangeFieldAttribute(string indexFile)
    {
        IndexFile = indexFile;
    }
}

public class PtriciaIndexAttribute : Attribute
{
    public string IndexFile { get; init; }
    public int    Offset    { get; init; }
    public int    Count     { get; init; }

    public PtriciaIndexAttribute(string indexFile)
    {
        IndexFile = indexFile;
    }
}

public class EntityFieldAttribute : Attribute
{
    public int Offset    { get; init; }
    public int EnitySize { get; init; }
}