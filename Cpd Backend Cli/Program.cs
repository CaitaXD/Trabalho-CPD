#define TEST_1
using System.Runtime.InteropServices;
using DataModel;


const string inputPath  = @"C:\Users\caita\Desktop\amazon.csv";
const string outputPath = @"C:\Users\caita\Desktop\Trab CPD\Files";

var records = CsvSerializer.Serialize<SalesCsv>(inputPath);


#if TEST_1
BinarySalesConverter.WriteSale("""
        "Begin!!!,\"\",\"\",\"\",\"\",\"\",\"\",\"\",\"\",1000,\"\",\"\",\"\",\"\",\"\",", "."
        """,
        outputPath, FileMode.Create);
BinarySalesConverter.CsvToBinaryFiles(inputPath, outputPath);
BinarySalesConverter.WriteSale("""
        "End!!!,\"\",\"\",\"\",\"\",\"\",\"\",\"\",\"\",1000,\"\",\"\",\"\",\"\",\"\",", "."
        """,
        outputPath);
var sales = BinarySalesConverter.BinaryFilesToObjects(outputPath);

Console.WriteLine(string.Join(Environment.NewLine, sales.Select(x => x.Product.product_id)));
#endif


#if TEST_0
FileSave.WriteObjects(ObjectSerializer.GetEntities<Product>(records), outputPath);
FileSave.WriteObjects(ObjectSerializer.GetEntities<Review>(records), outputPath);
FileSave.WriteObjects(ObjectSerializer.GetEntities<User>(records), outputPath);

var users = FileSave.ReadObjects<User>(outputPath);
var products = FileSave.ReadObjects<Product>(outputPath);
var reviews = FileSave.ReadObjects<Review>(outputPath);

Console.WriteLine(string.Join(Environment.NewLine, users));
Console.WriteLine(string.Join(Environment.NewLine, products));
Console.WriteLine(string.Join(Environment.NewLine, reviews));

#endif