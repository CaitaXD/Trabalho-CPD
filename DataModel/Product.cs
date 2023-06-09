﻿using System.Diagnostics.CodeAnalysis;
namespace DataModel;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public record Product 
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
}