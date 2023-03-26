using System.Globalization;
using System.Text;
using Data_Model;

const string inputPath  = @"C:\Users\caita\Desktop\amazon.csv";
const string outputPath = @"C:\Users\caita\Desktop\Trab CPD\Files";

var records = CsvSerializer.Serialize<Sale>(inputPath).ToArray();

var products = ObjectSerializer.GetRecords<Product>(records).ToArray();
var users    = ObjectSerializer.GetRecords<User>(records).ToArray();

Array.Sort(products, (p1, p2) => string.Compare(p1.product_name, p2.product_name, StringComparison.Ordinal));
Array.Sort(users, (u1,    u2) => string.Compare(u1.user_name, u2.user_name, StringComparison.Ordinal));

FileSave.WriteObjects(products, outputPath);
FileSave.WriteObjects(users, outputPath);

var p2 = FileSave.ReadRecords<Product>(outputPath).ToArray();
var u2 = FileSave.ReadRecords<User>(outputPath).ToArray();


Console.WriteLine("Products:");

foreach (var product in p2.Take(10)) {
    Console.WriteLine(product);
}

foreach(var user in u2.Take(10)) {
    Console.WriteLine(user);
}
                            

