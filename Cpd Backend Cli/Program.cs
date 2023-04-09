#define TEST_1
using System.Runtime.InteropServices;
using System.Text;
using DataModel;
using DataModel.DataStructures;
using DataModel.DataStructures.FileSystem;


#if TEST_1
const string inputPath = @"C:\Users\caita\Desktop\amazon.csv";
const string outputPath = @"C:\Users\caita\Desktop\Trab CPD\Files";


var records = CsvSerializer.Serialize<SalesCsv>(inputPath);
BinarySalesConverter.CsvToBinaryFiles(inputPath, outputPath, FileMode.OpenOrCreate);

var sales = BinarySalesConverter.BinaryFilesToObjects(outputPath);


var ordered_by_users = Query.Order(sales, "user_name")
    .Select(x => x.User!.user_name).ToArray();


foreach (var sale in ordered_by_users.ToArray())
{
    Console.WriteLine(sale);
}
Console.WriteLine(ordered_by_users.Length);

#endif

#if TEST_2

// var file = new FileStream(@"C:\Users\caita\Desktop\prefix_tree.bin", FileMode.Open);
// var prefix_tree = new PatriciaStream(file);
//
// Console.WriteLine(prefix_tree.PrettyString());

var     file     = new FileStream(@"C:\Users\caita\Desktop\Trab CPD\Files\UserNames.bin", FileMode.Open);

var trie = new PatriciaStream(file);

var arr = trie.Retrieve("").Select(x => trie.Encode(x)).ToArray();

Console.WriteLine(arr.Length);
Console.WriteLine(arr.Distinct().Count());

// foreach(var item in trie.Retrieve(""))
// {
//     Console.WriteLine(trie.Encode(item));
// }

Console.WriteLine(trie.Retrieve("A").Count());

#endif

