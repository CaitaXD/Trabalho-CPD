using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CsvHelper.Configuration.Attributes;
using JetBrains.Annotations;

namespace Data_Model;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public record Product //: IEquatable<Product>
{
    [SerialField(Offset = 0, Count = 10)] public string product_id { get; init; } = string.Empty;

    [RangeField("StringsIndex.bin", Offset = 10, Count = sizeof(long))]
    public string product_name { get; init; } = string.Empty;

    [RangeField("StringsIndex.bin", Offset = 10 + 1 * sizeof(long), Count = sizeof(long))]
    public string category { get; init; } = string.Empty;

    [RangeField("StringsIndex.bin", Offset = 10 + 2 * sizeof(long), Count = sizeof(long))]
    public string img_link { get; init; } = string.Empty;

    [RangeField("StringsIndex.bin", Offset = 10 + 3 * sizeof(long), Count = sizeof(long))]
    public string product_link { get; init; } = string.Empty;

    [RangeField("StringsIndex.bin", Offset = 10 + 4 * sizeof(long), Count = sizeof(long))]
    public string discounted_price { get; init; } = string.Empty;

    [RangeField("StringsIndex.bin", Offset = 10 + 5 * sizeof(long), Count = sizeof(long))]
    public string actual_price { get; init; } = string.Empty;

    [RangeField("StringsIndex.bin", Offset = 10 + 6 * sizeof(long), Count = sizeof(long))]
    public string discount_percentage { get; init; } = string.Empty;

    [RangeField("StringsIndex.bin", Offset = 10 + 7 * sizeof(long), Count = sizeof(long))]
    public string about_product { get; init; } = string.Empty;

    [SerialField(Offset = 10 + 8 * sizeof(long), Count = sizeof(int))]
    public int rating_count { get; init; }
    
    
    public virtual bool Equals(Product? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return product_id == other.product_id;
    }

    public override int GetHashCode()
    {
        return product_id.GetHashCode();
    }
}