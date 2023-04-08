using System.Diagnostics.CodeAnalysis;

namespace DataModel;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public record SalesCsv
{
    public string product_id          { get; init; } = string.Empty;
    public string product_name        { get; init; } = string.Empty;
    public string category            { get; init; } = string.Empty;
    public string img_link            { get; init; } = string.Empty;
    public string product_link        { get; init; } = string.Empty;
    public string discounted_price    { get; init; } = string.Empty;
    public string actual_price        { get; init; } = string.Empty;
    public string discount_percentage { get; init; } = string.Empty;
    public string about_product       { get; init; } = string.Empty;
    public int    rating_count        { get; init; }

    public string[] review_id { get; init; } = Array.Empty<string>();

    public string[] review_title        { get; init; } = Array.Empty<string>();
    public string[] review_content      { get; init; } = Array.Empty<string>();
    public string[] user_id   { get; init; } = Array.Empty<string>();
    public string[] user_name { get; init; } = Array.Empty<string>();
    
    
    public static SalesCsv Parse(string line)
    {
        string[]? values = line.Split(',');

        return new SalesCsv {
            product_id          = values[0],
            product_name        = values[1],
            category            = values[2],
            img_link            = values[3],
            product_link        = values[4],
            discounted_price    = values[5],
            actual_price        = values[6],
            discount_percentage = values[7],
            about_product       = values[8],
            rating_count        = int.Parse(values[9]),
            review_id           = values[10].Split(','),
            review_title        = values[11].Split(','),
            review_content      = values[12].Split(','),
            user_id             = values[13].Split(','),
            user_name           = values[14].Split(',')
        };
    }
}