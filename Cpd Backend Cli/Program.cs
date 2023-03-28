#define TEST_1
using System.Runtime.InteropServices;
using DataModel;


const string inputPath  = @"C:\Users\caita\Desktop\amazon.csv";
const string outputPath = @"C:\Users\caita\Desktop\Trab CPD\Files";

var records = CsvSerializer.Serialize<SalesCsv>(inputPath);


#if TEST_1

var in_memory = ObjectSerializer.GetEntities<Sale>(records);
FileSave.WriteObjects(in_memory, outputPath);


var sales = FileSave.ReadObjects<Sale>(outputPath).ToArray();


Console.WriteLine(string.Join(Environment.NewLine, sales.AsEnumerable()));
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