#define TEST_1
using System.Runtime.InteropServices;
using DataModel;


const string inputPath  = @"C:\Users\caita\Desktop\amazon.csv";
const string outputPath = @"C:\Users\caita\Desktop\Trab CPD\Files";

var records = CsvSerializer.Serialize<SalesCsv>(inputPath);


#if TEST_1

var sales = BinarySalesConverter.BinaryFilesToObjects(outputPath);


var ordered_by_users = Query.Order(sales, "user_name", true)
    .Select(x => x.User!.user_name);



foreach (var sale in ordered_by_users.Take(10))
{
    Console.WriteLine(sale);
}

#endif
