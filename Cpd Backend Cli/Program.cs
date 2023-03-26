using System.Globalization;
using System.Text;
using Data_Model;

const string inputPath  = @"C:\Users\caita\Desktop\amazon.csv";
const string outputPath = @"C:\Users\caita\Desktop\Trab CPD\Files";

var records = CsvSerializer.Serialize<Sale>(inputPath).ToArray();

var products = ObjectSerializer.GetRecords<Product>(records).ToArray();
var users    = ObjectSerializer.GetRecords<User>(records).ToArray();
var reviews  = ObjectSerializer.GetRecords<Review>(records).ToArray();

Array.Sort(products, (p1, p2) => string.Compare(p1.product_name, p2.product_name, StringComparison.Ordinal));
Array.Sort(users, (u1,    u2) => string.Compare(u1.user_name, u2.user_name, StringComparison.Ordinal));
Array.Sort(reviews, (r1,  r2) => string.Compare(r1.review_id, r2.review_id, StringComparison.Ordinal));

FileSave.WriteObjects(products, outputPath);
FileSave.WriteObjects(users, outputPath);
FileSave.WriteObjects(reviews, outputPath);

var p2 = FileSave.ReadRecords<Product>(outputPath);
var u2 = FileSave.ReadRecords<User>(outputPath);
var r2 = FileSave.ReadRecords<Review>(outputPath).ToArray();


foreach (var product in p2.Take(10)) {
    Console.WriteLine(product);
}

foreach (var user in u2.Take(10)) {
    Console.WriteLine(user);
}

foreach (var review in r2) {
    Console.WriteLine(review);
}