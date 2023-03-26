using System.Globalization;
using System.Text;
using Data_Model;

const string inputPath  = @"C:\Users\caita\Desktop\amazon.csv";
const string outputPath = @"C:\Users\caita\Desktop\Trab CPD\Files";

var records = CsvModule.Serialize<Sale>(inputPath).ToArray();

var products = CsvModule.GetRecords<Product>(records).ToArray();
var users    = CsvModule.GetRecords<User>(records).ToArray();

CsvModule.WriteObjects(products, outputPath);
CsvModule.WriteObjects(users, outputPath);

var p2 = CsvModule.ReadRecords<Product>(outputPath).ToArray();
var u2 = CsvModule.ReadRecords<User>(outputPath).ToArray();


//
