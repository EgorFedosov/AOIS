using System.Text;
using L4.Hashing;

Console.OutputEncoding = Encoding.UTF8;

var table = LiteratureHashTableFactory.CreatePreFilledTable();
Console.WriteLine(HashTableReportBuilder.Build(table));