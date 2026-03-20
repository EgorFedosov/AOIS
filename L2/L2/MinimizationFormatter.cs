using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace L2;

[ExcludeFromCodeCoverage]
public static class MinimizationFormatter
{
    public static string FormatCalculation(CalculationMethodResult result, IReadOnlyList<char> variables)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Initial SDNF: {result.InitialSdnf}");
        if (result.Stages.Count == 0)
        {
            builder.AppendLine($"Minimal DNF: {result.MinimalDnf}");
            return builder.ToString().TrimEnd();
        }

        foreach (var stage in result.Stages)
        {
            builder.AppendLine($"Step {stage.Step}:");
            builder.AppendLine(
                $"  Input: {string.Join(", ", stage.Input.Select(implicant => implicant.ToPattern(variables.Count)))}");
            if (stage.PairCombinations.Count > 0)
            {
                builder.AppendLine("  Combinations:");
                foreach (var line in stage.PairCombinations)
                {
                    builder.AppendLine($"    {line}");
                }
            }

            builder.AppendLine(stage.Output.Count == 0
                ? "  Output: none"
                : $"  Output: {string.Join(", ", stage.Output.Select(implicant => implicant.ToPattern(variables.Count)))}");
        }

        builder.AppendLine(
            $"Prime implicants: {string.Join(", ", result.PrimeImplicants.Select(implicant => implicant.ToTerm(variables)))}");
        builder.AppendLine($"Minimal DNF: {result.MinimalDnf}");
        return builder.ToString().TrimEnd();
    }

    public static string FormatCoverageTable(CoverageTable table, IReadOnlyList<char> variables)
    {
        var builder = new StringBuilder();
        builder.Append("| Implicant | ");
        builder.Append(string.Join(" | ", table.Minterms));
        builder.AppendLine(" |");
        builder.Append("|---|");
        builder.Append(string.Join("", Enumerable.Repeat("---|", table.Minterms.Count)));
        builder.AppendLine();

        for (var row = 0; row < table.Implicants.Count; row++)
        {
            builder.Append($"| {table.Implicants[row].ToTerm(variables)} | ");
            var marks = new string[table.Minterms.Count];
            for (var column = 0; column < table.Minterms.Count; column++)
            {
                marks[column] = table.Matrix[row, column] ? "X" : " ";
            }

            builder.Append(string.Join(" | ", marks));
            builder.AppendLine(" |");
        }

        return builder.ToString().TrimEnd();
    }

    public static string FormatKarnaugh(KarnaughMapResult map)
    {
        if (map.Note is not null)
        {
            return map.Note;
        }

        var builder = new StringBuilder();
        var rowHeader = map.RowVariables.Count == 0 ? "-" : string.Join("", map.RowVariables);
        var columnHeader = map.ColumnVariables.Count == 0 ? "-" : string.Join("", map.ColumnVariables);
        builder.AppendLine($"Map: {rowHeader} \\ {columnHeader}");
        builder.Append("|   | ");
        builder.Append(string.Join(" | ", map.ColumnLabels));
        builder.AppendLine(" |");
        builder.Append("|---|");
        builder.Append(string.Join("", Enumerable.Repeat("---|", map.ColumnLabels.Count)));
        builder.AppendLine();

        for (var row = 0; row < map.RowLabels.Count; row++)
        {
            builder.Append($"| {map.RowLabels[row]} | ");
            var values = new string[map.ColumnLabels.Count];
            for (var column = 0; column < map.ColumnLabels.Count; column++)
            {
                values[column] = map.Values[row, column] ? "1" : "0";
            }

            builder.Append(string.Join(" | ", values));
            builder.AppendLine(" |");
        }

        if (map.Groups.Count > 0)
        {
            builder.AppendLine("Groups:");
            foreach (var group in map.Groups)
            {
                builder.AppendLine($"  {group.Name}: {group.Term} -> [{string.Join(", ", group.CoveredCells)}]");
            }
        }

        builder.AppendLine($"Minimal DNF: {map.MinimalDnf}");
        return builder.ToString().TrimEnd();
    }
}
