using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace L2;

[ExcludeFromCodeCoverage]
public static class ReportFormatter
{
    public static string FormatTruthTable(TruthTable table)
    {
        var builder = new StringBuilder();
        builder.Append("| # | ");
        builder.Append(string.Join(" | ", table.Variables));
        builder.AppendLine(" | f |");
        builder.Append("|---|");
        builder.Append(string.Join("", Enumerable.Repeat("---|", table.VariableCount + 1)));
        builder.AppendLine();

        foreach (var row in table.Rows)
        {
            builder.Append($"| {row.Index} | ");
            builder.Append(string.Join(" | ", row.Inputs.Select(value => value ? "1" : "0")));
            builder.Append($" | {(row.Value ? "1" : "0")} |");
            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    public static string FormatNumericForm(string symbol, IReadOnlyList<int> indices) =>
        indices.Count == 0
            ? $"{symbol}()"
            : $"{symbol}({string.Join(", ", indices)})";

    public static string FormatPostClasses(PostClasses classes) =>
        $"T0={ToInt(classes.T0)}, T1={ToInt(classes.T1)}, S={ToInt(classes.S)}, M={ToInt(classes.M)}, L={ToInt(classes.L)}";

    public static string FormatDerivativeName(IReadOnlyList<char> variables) =>
        $"d/d({string.Join(",", variables)})";

    public static string FormatTruthVector(IReadOnlyList<bool> values) =>
        new(values.Select(value => value ? '1' : '0').ToArray());

    private static int ToInt(bool value) => value ? 1 : 0;
}
