﻿using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace DataModel;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public record User
{
    [SerialField(Offset = 0, Count = 28)] public string user_id { get; init; } = string.Empty;

    [PatriciaField("UserNames.bin", Offset = 28, Count = sizeof(long))]
    //[RangeField("StringsIndex.bin", Offset = 28, Count = sizeof(long))]
    public string user_name { get; init; } = string.Empty;
}