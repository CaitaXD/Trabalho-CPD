using System.Collections;
using JetBrains.Annotations;

namespace DataModel;

public record Sale
{
    [EntityField(Offset = 0, EnitySize = 2 * sizeof(long) + 14)]
    [PublicAPI]
    public Review? Review { get; init; }

    [EntityField(Offset = 4, EnitySize = 8 * sizeof(long) + sizeof(int) + 10)]
    [PublicAPI]
    public Product? Product { get; init; }

    [EntityField(Offset = 8, EnitySize = sizeof(long) + 28)]
    [PublicAPI]
    public User? User { get; init; }
    
}