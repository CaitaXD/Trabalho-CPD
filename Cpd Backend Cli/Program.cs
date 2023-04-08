﻿using System.Runtime.InteropServices;
using DataModel;
using DataModel.DataStructures.Generic;

#if TEST_1
const string inputPath = @"C:\Users\caita\Desktop\amazon.csv";
const string outputPath = @"C:\Users\caita\Desktop\Trab CPD\Files";
var records = CsvSerializer.Serialize<SalesCsv>(inputPath);
BinarySalesConverter.CsvToBinaryFiles(inputPath, outputPath, FileMode.Create);

var sales = BinarySalesConverter.BinaryFilesToObjects(outputPath);

var ordered_by_users = Query.Order(sales, "user_name", true)
    .Select(x => x.User!.user_name);

foreach (var sale in ordered_by_users.Take(10))
{
    Console.WriteLine(sale);
}
#endif

var prefix_tree = new PatriciaTrie<char>
{
    "Bob",
    "Bobby",
    "Bobby Tables",
    "Bobby Tables (on drugs)",
    "Alice",
    "Alice in Wonderland",
    "Help",
    "Help me",
    "Help me Obi-Wan Kenobi",
    "Help me Obi-Wan Kenobi, you're my only hope",
    "Hello",
    "Hello World",
    "Hello World!",
    "Hello World! (on drugs)",
    "Hello, There",
    "Hello, There",
    "Hello, There!",
    "General Kenobi",
};

prefix_tree.Write(@"C:\Users\caita\Desktop\prefix_tree.bin");

var prefix_tree2 = new PatriciaTrie<char>();
prefix_tree2.Read(@"C:\Users\caita\Desktop\prefix_tree.bin");

Console.WriteLine(prefix_tree2.PrettyString());

foreach (var item in prefix_tree2.Retrieve("H")) {
    Console.WriteLine(item);
}