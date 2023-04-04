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
        FileSave.WriteObjects(ObjectSerializer.GetEntities<Sale>(new[]{SalesCsv.Parse(inlineCsv)}), outputDirectory, fileMode);
    }
}