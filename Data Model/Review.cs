using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Data_Model;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Review
{
    public string review_id { get; init; }
    
    public Range review_title { get; init; }

    public Range review_content { get; init; }
}