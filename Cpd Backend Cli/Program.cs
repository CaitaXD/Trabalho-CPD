#define TEST_2
using System.Runtime.InteropServices;
using System.Text;
using DataModel;
using DataModel.DataStructures;
using DataModel.DataStructures.FileSystem;


#if TEST_1
const string inputPath = @"C:\Users\caita\Desktop\amazon.csv";
const string outputPath = @"C:\Users\caita\Desktop\Trab CPD\Files";


var records = CsvSerializer.Serialize<SalesCsv>(inputPath);
BinarySalesConverter.CsvToBinaryFiles(inputPath, outputPath);

var sales = BinarySalesConverter.BinaryFilesToObjects(outputPath);


var ordered_by_users = Query.Order(sales, "user_name", true)
    .Select(x => x.User!.user_name);



foreach (var sale in ordered_by_users.Take(10))
{
    Console.WriteLine(sale);
}

#endif

#if TEST_2

// var file = new FileStream(@"C:\Users\caita\Desktop\prefix_tree.bin", FileMode.Open);
// var prefix_tree = new PatriciaStream(file);
//
// Console.WriteLine(prefix_tree.PrettyString());


var trie = new Patricia
{
    "A",
    "Acolyte",
    "Acolyte of the Void",
    "Archer",
    "Bills",
    "Bills of the Void",
    "Bills of the Void Archer",
    "Void",
    "Void Archer",
    "Void Archer Acolyte",
    "Hello",
    "Hello World",
    "Hurricane",
};

Console.WriteLine(string.Join("\n", trie));

Console.WriteLine(trie.PrettyString());

Console.WriteLine(string.Join(", ", trie.Retrieve("A")));
#endif