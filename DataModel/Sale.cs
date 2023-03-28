namespace DataModel;

public record Sale
{
    [EntityField(Offset = 0, EnitySize = 2 * sizeof(long) + 14)]
    public Review Review { get; init; }

    [EntityField(Offset = 4, EnitySize = 8 * sizeof(long) + sizeof(int) + 10)]
    public Product Product { get; init; }

    [EntityField(Offset = 8, EnitySize = sizeof(long) + 28)]
    public User User { get; init; }
}