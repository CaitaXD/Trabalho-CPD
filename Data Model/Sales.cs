using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Data_Model;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public record Sale
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

    //public string[] review_title        { get; init; } = Array.Empty<string>();
    //public string[] review_content      { get; init; } = Array.Empty<string>();
    public string[] user_id   { get; init; } = Array.Empty<string>();
    public string[] user_name { get; init; } = Array.Empty<string>();
}