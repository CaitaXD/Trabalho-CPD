using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using RGiesecke.DllExport;

namespace DataModel;

public static class BinarySalesConverter
{
    [DllExport("CsvToBinaryFiles", CallingConvention = CallingConvention.Cdecl)]
    public static void CsvToBinaryFiles(string csvFile, string outputDirectory, FileMode fileMode = FileMode.Append)
    {
        var records   = CsvSerializer.Serialize<SalesCsv>(csvFile);
        var formatted = ObjectSerializer.GetEntities<Sale>(records);
        FileSave.WriteObjects(formatted, outputDirectory, fileMode);
    }

    [DllExport("BinaryFilesToObjects", CallingConvention = CallingConvention.Cdecl)]
    public static IEnumerable<Sale> BinaryFilesToObjects(string directory)
    {
        return FileSave.ReadObjects<Sale>(directory);
    }

    [DllExport("WriteSale", CallingConvention = CallingConvention.Cdecl)]
    public static void WriteSale(string inlineCsv, string outputDirectory, FileMode fileMode = FileMode.Append)
    {
        FileSave.WriteObjects(ObjectSerializer.GetEntities<Sale>(new[] { SalesCsv.Parse(inlineCsv) }), outputDirectory,
            fileMode);
    }
}

public static class Query
{
    [DllExport("GetProprietyValueRecursive", CallingConvention = CallingConvention.Cdecl)]
    static object? GetProprietyValueRecursive(object obj, string name)
    {
        var type = obj.GetType();

        if (type.IsPrimitive || type == typeof(string)) {
            return null;
        }

        foreach (var property in type.GetProperties()) {
            if (property.Name == name) {
                return property.GetValue(obj);
            }

            if (GetProprietyValueRecursive(property.GetValue(obj)!, name) is { } nested_property) {
                return nested_property;
            }
        }

        return null;
    }

    [DllExport("Order", CallingConvention = CallingConvention.Cdecl)]
    public static IEnumerable<Sale> Order(IEnumerable<Sale> sales, string field, bool ascending = true)
    {
        object? KeySelector(Sale sale) => GetProprietyValueRecursive(sale, field);

        return ascending ? sales.OrderBy(KeySelector) : sales.OrderByDescending(KeySelector);
    }
}