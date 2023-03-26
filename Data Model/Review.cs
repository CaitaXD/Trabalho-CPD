using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Data_Model;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public record Review
{
    [SerialField(Offset = 0, Count = 14)] public string review_id { get; init; } = string.Empty;

    //[RangeField("StringsIndex.bin", Offset = 14, Count = sizeof(long))]
    //public string review_title { get; init; } = string.Empty;

    //[RangeField("StringsIndex.bin", Offset = 22, Count = sizeof(long))]
    //public string review_content { get; init; } = string.Empty;
}