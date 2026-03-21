using System.Numerics;

namespace L2;

public sealed class Implicant : IEquatable<Implicant>
{
    public Implicant(int value, int mask, IEnumerable<int> coveredMinterms)
    {
        Value = value;
        Mask = mask;
        CoveredMinterms = coveredMinterms.Distinct().OrderBy(minterm => minterm).ToArray();
    }

    public int Value { get; }
    public int Mask { get; }
    private IReadOnlyList<int> CoveredMinterms { get; }

    public int LiteralCount(int variableCount) => variableCount - BitOperations.PopCount((uint)Mask);

    public bool Covers(int minterm) => (minterm & ~Mask) == (Value & ~Mask);

    public bool CanCombineWith(Implicant other, out int differenceBit)
    {
        differenceBit = 0;
        if (Mask != other.Mask)
        {
            return false;
        }

        var difference = Value ^ other.Value;
        if (difference == 0 || !IsPowerOfTwo(difference))
        {
            return false;
        }

        if ((difference & Mask) != 0)
        {
            return false;
        }

        differenceBit = difference;
        return true;
    }

    public Implicant CombineWith(Implicant other, int differenceBit)
    {
        var nextMask = Mask | differenceBit;
        var nextValue = Value & ~differenceBit;
        return new Implicant(nextValue, nextMask, CoveredMinterms.Concat(other.CoveredMinterms));
    }

    public string ToPattern(int variableCount)
    {
        var symbols = new char[variableCount];
        for (var variableIndex = 0; variableIndex < variableCount; variableIndex++)
        {
            var bitPosition = BitIndex.GetBitPosition(variableIndex, variableCount);
            var bitMask = 1 << bitPosition;
            if ((Mask & bitMask) != 0)
            {
                symbols[variableIndex] = 'X';
                continue;
            }

            symbols[variableIndex] = (Value & bitMask) != 0 ? '1' : '0';
        }

        return new string(symbols);
    }

    public string ToTerm(IReadOnlyList<char> variables)
    {
        var literals = new List<string>();
        for (var variableIndex = 0; variableIndex < variables.Count; variableIndex++)
        {
            var bitMask = 1 << BitIndex.GetBitPosition(variableIndex, variables.Count);
            if ((Mask & bitMask) != 0)
            {
                continue;
            }

            literals.Add((Value & bitMask) != 0 ? variables[variableIndex].ToString() : $"!{variables[variableIndex]}");
        }

        if (literals.Count == 0)
        {
            return "1";
        }

        return literals.Count == 1 ? literals[0] : $"({string.Join(" & ", literals)})";
    }

    public string ToClause(IReadOnlyList<char> variables)
    {
        var literals = new List<string>();
        for (var variableIndex = 0; variableIndex < variables.Count; variableIndex++)
        {
            var bitMask = 1 << BitIndex.GetBitPosition(variableIndex, variables.Count);
            if ((Mask & bitMask) != 0)
            {
                continue;
            }

            literals.Add((Value & bitMask) != 0 ? $"!{variables[variableIndex]}" : variables[variableIndex].ToString());
        }

        if (literals.Count == 0)
        {
            return "0";
        }

        return $"({string.Join(" | ", literals)})";
    }

    public bool Equals(Implicant? other) => other is not null && Value == other.Value && Mask == other.Mask;

    public override bool Equals(object? obj) => obj is Implicant other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Value, Mask);

    private static bool IsPowerOfTwo(int value) => (value & (value - 1)) == 0;
}

public sealed record GluingStage(
    int Step,
    IReadOnlyList<Implicant> Input,
    IReadOnlyList<string> PairCombinations,
    IReadOnlyList<Implicant> Output);

public sealed record CalculationMethodResult(
    string InitialSdnf,
    IReadOnlyList<int> Minterms,
    IReadOnlyList<GluingStage> Stages,
    IReadOnlyList<Implicant> PrimeImplicants,
    IReadOnlyList<Implicant> SelectedImplicants,
    string MinimalDnf);

public sealed record CoverageTable(
    IReadOnlyList<Implicant> Implicants,
    IReadOnlyList<int> Minterms,
    bool[,] Matrix);

public sealed record CalculationTableMethodResult(
    CalculationMethodResult BaseResult,
    CoverageTable Table);

public sealed record KarnaughGroup(string Name, IReadOnlyList<int> CoveredCells, string Term);

public sealed record KarnaughMapResult(
    IReadOnlyList<char> RowVariables,
    IReadOnlyList<char> ColumnVariables,
    IReadOnlyList<string> RowLabels,
    IReadOnlyList<string> ColumnLabels,
    bool[,] Values,
    IReadOnlyList<KarnaughGroup> Groups,
    string MinimalDnf,
    string? Note);

public static class BooleanMinimizer
{
    public static CalculationMethodResult MinimizeCalculation(TruthTable table) =>
        MinimizeCalculationInternal(table, minimizeOnes: true);

    public static CalculationMethodResult MinimizeCalculationSknf(TruthTable table) =>
        MinimizeCalculationInternal(table, minimizeOnes: false);

    public static CalculationTableMethodResult MinimizeCalculationTable(TruthTable table)
    {
        var calculation = MinimizeCalculation(table);
        var tableCoverage = BuildCoverageTable(calculation.PrimeImplicants, calculation.Minterms);
        return new CalculationTableMethodResult(calculation, tableCoverage);
    }

    public static CalculationTableMethodResult MinimizeCalculationTableSknf(TruthTable table)
    {
        var calculation = MinimizeCalculationSknf(table);
        var tableCoverage = BuildCoverageTable(calculation.PrimeImplicants, calculation.Minterms);
        return new CalculationTableMethodResult(calculation, tableCoverage);
    }

    public static KarnaughMapResult MinimizeKarnaugh(TruthTable table) =>
        MinimizeKarnaughInternal(table, minimizeOnes: true);

    public static KarnaughMapResult MinimizeKarnaughSknf(TruthTable table) =>
        MinimizeKarnaughInternal(table, minimizeOnes: false);

    private static CalculationMethodResult MinimizeCalculationInternal(TruthTable table, bool minimizeOnes)
    {
        var targetIndices = table.Rows
            .Where(row => row.Value == minimizeOnes)
            .Select(row => row.Index)
            .ToArray();
        var canonical = CanonicalFormBuilder.Build(table);
        var initialForm = minimizeOnes ? canonical.Sdnf : canonical.Sknf;

        if (targetIndices.Length == 0)
        {
            return new CalculationMethodResult(initialForm, targetIndices, [], [], [], minimizeOnes ? "0" : "1");
        }

        if (targetIndices.Length == table.RowCount)
        {
            var universal = new Implicant(0, (1 << table.VariableCount) - 1, targetIndices);
            return new CalculationMethodResult(
                initialForm,
                targetIndices,
                [],
                [universal],
                [universal],
                minimizeOnes ? "1" : "0");
        }

        var (stages, primeImplicants) = BuildPrimeImplicants(targetIndices, table.VariableCount);
        var selected = SelectMinimalCover(primeImplicants, targetIndices, table.VariableCount);
        var minimalForm = BuildMinimalForm(selected, table.Variables, minimizeOnes);
        return new CalculationMethodResult(initialForm, targetIndices, stages, primeImplicants, selected, minimalForm);
    }

    private static KarnaughMapResult MinimizeKarnaughInternal(TruthTable table, bool minimizeOnes)
    {
        var calculation = minimizeOnes ? MinimizeCalculation(table) : MinimizeCalculationSknf(table);

        var (rowVariables, columnVariables) = ResolveMapVariableSplit(table.Variables);
        var rowCodes = BuildGrayCodes(rowVariables.Count);
        var columnCodes = BuildGrayCodes(columnVariables.Count);
        var values = new bool[rowCodes.Count, columnCodes.Count];
        var indexToCell = new Dictionary<int, (int Row, int Column)>();

        for (var row = 0; row < rowCodes.Count; row++)
        {
            for (var column = 0; column < columnCodes.Count; column++)
            {
                var index = ComposeRowIndex(rowCodes[row], columnCodes[column], rowVariables.Count,
                    columnVariables.Count, table.VariableCount);
                values[row, column] = table.Values[index];
                indexToCell[index] = (row, column);
            }
        }

        var groups = new List<KarnaughGroup>();
        for (var i = 0; i < calculation.SelectedImplicants.Count; i++)
        {
            var implicant = calculation.SelectedImplicants[i];
            var coveredCells = new List<int>();
            for (var index = 0; index < table.RowCount; index++)
            {
                if (!implicant.Covers(index) || table.Values[index] != minimizeOnes)
                {
                    continue;
                }

                var cell = indexToCell[index];
                coveredCells.Add(cell.Row * columnCodes.Count + cell.Column);
            }

            groups.Add(new KarnaughGroup($"K{i + 1}", coveredCells.Distinct().OrderBy(cell => cell).ToArray(),
                minimizeOnes ? implicant.ToTerm(table.Variables) : implicant.ToClause(table.Variables)));
        }

        return new KarnaughMapResult(
            rowVariables,
            columnVariables,
            rowCodes.Select(code => ToBinaryLabel(code, rowVariables.Count)).ToArray(),
            columnCodes.Select(code => ToBinaryLabel(code, columnVariables.Count)).ToArray(),
            values,
            groups,
            calculation.MinimalDnf,
            null);
    }

    private static string BuildMinimalForm(
        IReadOnlyList<Implicant> selected,
        IReadOnlyList<char> variables,
        bool minimizeOnes)
    {
        if (selected.Count == 0)
        {
            return minimizeOnes ? "0" : "1";
        }

        return minimizeOnes
            ? string.Join(" | ", selected.Select(implicant => implicant.ToTerm(variables)))
            : string.Join(" & ", selected.Select(implicant => implicant.ToClause(variables)));
    }

    private static (IReadOnlyList<GluingStage> Stages, IReadOnlyList<Implicant> PrimeImplicants) BuildPrimeImplicants(
        IReadOnlyList<int> minterms,
        int variableCount)
    {
        var current = minterms.Select(minterm => new Implicant(minterm, 0, [minterm])).ToArray();
        var stages = new List<GluingStage>();
        var primeImplicants = new HashSet<Implicant>();
        var step = 1;

        while (current.Length > 0)
        {
            var used = new bool[current.Length];
            var combined = new Dictionary<(int Value, int Mask), Implicant>();
            var pairDescriptions = new List<string>();

            for (var left = 0; left < current.Length; left++)
            {
                for (var right = left + 1; right < current.Length; right++)
                {
                    var first = current[left];
                    var second = current[right];
                    if (!first.CanCombineWith(second, out var differenceBit))
                    {
                        continue;
                    }

                    used[left] = true;
                    used[right] = true;
                    var next = first.CombineWith(second, differenceBit);
                    combined[(next.Value, next.Mask)] = next;
                    pairDescriptions.Add(
                        $"{first.ToPattern(variableCount)} + {second.ToPattern(variableCount)} => {next.ToPattern(variableCount)}");
                }
            }

            for (var i = 0; i < current.Length; i++)
            {
                if (!used[i])
                {
                    primeImplicants.Add(current[i]);
                }
            }

            var output = combined.Values
                .OrderBy(implicant => implicant.Mask)
                .ThenBy(implicant => implicant.Value)
                .ToArray();

            stages.Add(new GluingStage(step, current, pairDescriptions, output));

            if (output.Length == 0)
            {
                break;
            }

            current = output;
            step++;
        }

        var orderedPrimes = primeImplicants
            .OrderBy(implicant => implicant.Mask)
            .ThenBy(implicant => implicant.Value)
            .ToArray();
        return (stages, orderedPrimes);
    }

    private static IReadOnlyList<Implicant> SelectMinimalCover(
        IReadOnlyList<Implicant> primeImplicants,
        IReadOnlyList<int> minterms,
        int variableCount)
    {
        var mintermIndex = minterms
            .Select((value, position) => new { value, position })
            .ToDictionary(item => item.value, item => item.position);

        var coverageMasks = new ulong[primeImplicants.Count];
        for (var i = 0; i < primeImplicants.Count; i++)
        {
            ulong mask = 0;
            foreach (var minterm in minterms)
            {
                if (!primeImplicants[i].Covers(minterm))
                {
                    continue;
                }

                mask |= 1UL << mintermIndex[minterm];
            }

            coverageMasks[i] = mask;
        }

        var targetMask = minterms.Count >= 64
            ? ulong.MaxValue
            : (1UL << minterms.Count) - 1UL;

        var essential = new HashSet<int>();
        for (var mintermPosition = 0; mintermPosition < minterms.Count; mintermPosition++)
        {
            var mintermMask = 1UL << mintermPosition;
            var owners = Enumerable.Range(0, primeImplicants.Count)
                .Where(index => (coverageMasks[index] & mintermMask) != 0)
                .ToArray();

            if (owners.Length == 1)
            {
                essential.Add(owners[0]);
            }
        }

        var coveredByEssential = essential.Aggregate(0UL, (current, index) => current | coverageMasks[index]);
        var remainingTarget = targetMask & ~coveredByEssential;

        if (remainingTarget == 0)
        {
            return essential.Select(index => primeImplicants[index])
                .OrderBy(implicant => implicant.LiteralCount(variableCount))
                .ThenBy(implicant => implicant.Mask)
                .ThenBy(implicant => implicant.Value)
                .ToArray();
        }

        var available = Enumerable.Range(0, primeImplicants.Count)
            .Where(index => !essential.Contains(index) && (coverageMasks[index] & remainingTarget) != 0)
            .ToArray();

        var optionsByMinterm = BuildOptionsByMinterm(available, coverageMasks, minterms.Count);
        var selected = SearchBestCover(available, optionsByMinterm, coverageMasks, remainingTarget, primeImplicants,
            variableCount);

        var finalSelection = essential.Concat(selected).Distinct()
            .Select(index => primeImplicants[index])
            .OrderBy(implicant => implicant.LiteralCount(variableCount))
            .ThenBy(implicant => implicant.Mask)
            .ThenBy(implicant => implicant.Value)
            .ToArray();

        return finalSelection;
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<int>> BuildOptionsByMinterm(
        IReadOnlyList<int> available,
        IReadOnlyList<ulong> coverageMasks,
        int mintermCount)
    {
        var result = new Dictionary<int, IReadOnlyList<int>>();
        for (var mintermPosition = 0; mintermPosition < mintermCount; mintermPosition++)
        {
            var mintermMask = 1UL << mintermPosition;
            var options = available.Where(index => (coverageMasks[index] & mintermMask) != 0).ToArray();
            result[mintermPosition] = options;
        }

        return result;
    }

    private static IReadOnlyList<int> SearchBestCover(
        IReadOnlyList<int> available,
        IReadOnlyDictionary<int, IReadOnlyList<int>> optionsByMinterm,
        IReadOnlyList<ulong> coverageMasks,
        ulong targetMask,
        IReadOnlyList<Implicant> primeImplicants,
        int variableCount)
    {
        _ = available;
        var best = Array.Empty<int>();
        var bestFound = false;
        var bestLiteralCount = int.MaxValue;
        var literalCounts = primeImplicants.Select(implicant => implicant.LiteralCount(variableCount)).ToArray();
        var chosen = new List<int>();
        var chosenSet = new HashSet<int>();
        var chosenLiteralCount = 0;

        Explore(0UL);
        return best;

        void Explore(ulong covered)
        {
            if (bestFound)
            {
                if (chosen.Count > best.Length)
                {
                    return;
                }

                if (chosen.Count == best.Length && chosenLiteralCount >= bestLiteralCount)
                {
                    return;
                }
            }

            if ((covered & targetMask) == targetMask)
            {
                var current = chosen.OrderBy(index => index).ToArray();
                if (bestFound &&
                    current.Length >= best.Length &&
                    (current.Length != best.Length || chosenLiteralCount >= bestLiteralCount))
                {
                    return;
                }

                best = current;
                bestLiteralCount = chosenLiteralCount;
                bestFound = true;
                return;
            }

            var uncoveredMask = targetMask & ~covered;
            var nextMinterm = FindFirstSetBit(uncoveredMask);
            if (nextMinterm < 0)
            {
                return;
            }

            var options = optionsByMinterm[nextMinterm];
            if (options.Count == 0)
            {
                return;
            }

            foreach (var candidate in options
                         .OrderByDescending(index => BitOperations.PopCount(coverageMasks[index] & uncoveredMask))
                         .ThenBy(index => literalCounts[index])
                         .ThenBy(index => index))
            {
                if (chosenSet.Contains(candidate))
                {
                    continue;
                }

                chosen.Add(candidate);
                chosenSet.Add(candidate);
                chosenLiteralCount += literalCounts[candidate];
                Explore(covered | coverageMasks[candidate]);
                chosenLiteralCount -= literalCounts[candidate];
                chosenSet.Remove(candidate);
                chosen.RemoveAt(chosen.Count - 1);
            }
        }
    }

    private static int FindFirstSetBit(ulong value)
    {
        if (value == 0)
        {
            return -1;
        }

        var index = 0;
        while ((value & 1UL) == 0)
        {
            index++;
            value >>= 1;
        }

        return index;
    }

    private static CoverageTable BuildCoverageTable(IReadOnlyList<Implicant> implicants, IReadOnlyList<int> minterms)
    {
        var matrix = new bool[implicants.Count, minterms.Count];
        for (var row = 0; row < implicants.Count; row++)
        {
            for (var column = 0; column < minterms.Count; column++)
            {
                matrix[row, column] = implicants[row].Covers(minterms[column]);
            }
        }

        return new CoverageTable(implicants, minterms, matrix);
    }

    private static (IReadOnlyList<char> Rows, IReadOnlyList<char> Columns) ResolveMapVariableSplit(
        IReadOnlyList<char> variables)
    {
        return variables.Count switch
        {
            1 => ([], [variables[0]]),
            2 => ([variables[0]], [variables[1]]),
            3 => ([variables[0]], [variables[1], variables[2]]),
            4 => ([variables[0], variables[1]], [variables[2], variables[3]]),
            5 => ([variables[0], variables[1]], [variables[2], variables[3], variables[4]]),
            _ => throw new ArgumentOutOfRangeException(nameof(variables), "Karnaugh map supports 1..5 variables.")
        };
    }

    private static IReadOnlyList<int> BuildGrayCodes(int bits)
    {
        if (bits == 0)
        {
            return [0];
        }

        var size = 1 << bits;
        var codes = new int[size];
        for (var value = 0; value < size; value++)
        {
            codes[value] = value ^ (value >> 1);
        }

        return codes;
    }

    private static int ComposeRowIndex(
        int rowCode,
        int columnCode,
        int rowVariableCount,
        int columnVariableCount,
        int totalVariableCount)
    {
        var result = 0;
        for (var variableIndex = 0; variableIndex < rowVariableCount; variableIndex++)
        {
            var bit = (rowCode >> (rowVariableCount - 1 - variableIndex)) & 1;
            if (bit == 1)
            {
                result |= 1 << BitIndex.GetBitPosition(variableIndex, totalVariableCount);
            }
        }

        for (var variableIndex = 0; variableIndex < columnVariableCount; variableIndex++)
        {
            var bit = (columnCode >> (columnVariableCount - 1 - variableIndex)) & 1;
            if (bit != 1) continue;
            var absoluteVariableIndex = rowVariableCount + variableIndex;
            result |= 1 << BitIndex.GetBitPosition(absoluteVariableIndex, totalVariableCount);
        }

        return result;
    }

    private static string ToBinaryLabel(int value, int bits)
    {
        if (bits == 0)
        {
            return "0";
        }

        var chars = new char[bits];
        for (var bit = 0; bit < bits; bit++)
        {
            chars[bit] = ((value >> (bits - 1 - bit)) & 1) == 1 ? '1' : '0';
        }

        return new string(chars);
    }
}
