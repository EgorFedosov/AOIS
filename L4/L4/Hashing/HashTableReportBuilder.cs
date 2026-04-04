using System.Text;

namespace L4.Hashing;

public static class HashTableReportBuilder
{
    public static string Build(HashTable table)
    {
        ArgumentNullException.ThrowIfNull(table);

        var report = new StringBuilder();
        AppendHeader(report, table);
        AppendBucketSection(report, table.GetBucketRows());
        AppendEntrySection(report, table.GetRows());

        return report.ToString();
    }

    private static void AppendHeader(StringBuilder report, HashTable table)
    {
        report.AppendLine("ХЕШ-ТАБЛИЦА (вариант 6: цепочки на сбалансированном дереве)");
        report.AppendLine($"Размер (H): {table.Capacity}");
        report.AppendLine($"Количество занятых строк: {table.Count}");
        report.AppendLine($"Коэффициент заполнения: {table.GetLoadFactor():F2}");
        report.AppendLine();
    }

    private static void AppendBucketSection(StringBuilder report, IReadOnlyList<HashBucketRow> bucketRows)
    {
        report.AppendLine("Состояние бакетов:");
        report.AppendLine("№ | Root | Active | Total | C");
        foreach (var row in bucketRows)
        {
            report.AppendLine(
                $"{row.BucketIndex,2} | {row.RootKey,-12} | {row.ActiveCount,6} | {row.TotalCount,5} | {ToFlag(row.Collision)}");
        }

        report.AppendLine();
    }

    private static void AppendEntrySection(StringBuilder report, IReadOnlyList<HashTableRow> rows)
    {
        report.AppendLine("Строки таблицы:");
        report.AppendLine("h | ID | V | C | U | T | L | D | Po | Pi");
        foreach (var row in rows)
        {
            report.AppendLine(
                $"{row.HashAddress,2} | {row.Key,-12} | {row.NumericValue,4} | {ToFlag(row.Collision)} | {ToFlag(row.Occupied)} | {ToFlag(row.Terminal)} | {ToFlag(row.Link)} | {ToFlag(row.Deleted)} | {row.OverflowPointer,-18} | {row.Data}");
        }
    }

    private static int ToFlag(bool value)
    {
        return value ? 1 : 0;
    }
}