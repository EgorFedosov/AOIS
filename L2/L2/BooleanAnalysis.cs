using System.Numerics;

namespace L2;

public sealed class TruthTableRow(int index, bool[] inputs, bool value)
{
    public int Index { get; } = index;
    public IReadOnlyList<bool> Inputs { get; } = inputs;
    public bool Value { get; } = value;
}

public sealed class TruthTable
{
    private readonly IReadOnlyList<bool> _values;

    public TruthTable(IReadOnlyList<char> variables, IReadOnlyList<TruthTableRow> rows, bool[] values)
    {
        Variables = variables;
        Rows = rows;
        _values = Array.AsReadOnly(values.ToArray());
    }

    public IReadOnlyList<char> Variables { get; }
    public IReadOnlyList<TruthTableRow> Rows { get; }
    public IReadOnlyList<bool> Values => _values;
    public int VariableCount => Variables.Count;
    public int RowCount => Rows.Count;
    public string TruthVector => new(Values.Select(value => value ? '1' : '0').ToArray());
}

public sealed record CanonicalForms(
    string Sdnf,
    string Sknf,
    IReadOnlyList<int> SdnfNumericForm,
    IReadOnlyList<int> SknfNumericForm,
    ulong IndexValue,
    string TruthVector);

public sealed record PostClasses(bool T0, bool T1, bool S, bool M, bool L);

public sealed record ZhegalkinPolynomial(string Expression, bool[] Coefficients);

public sealed record DerivativeResult(IReadOnlyList<char> ByVariables, bool[] Values, string Sdnf);

public static class TruthTableBuilder
{
    private const int MaxVariableCount = 5;

    public static TruthTable Build(BoolExpr expression)
    {
        var variables = CollectVariables(expression);
        var rowCount = 1 << variables.Count;
        var rows = new List<TruthTableRow>(rowCount);
        var values = new bool[rowCount];

        var assignment = new Dictionary<char, bool>(variables.Count);
        for (var index = 0; index < rowCount; index++)
        {
            var inputs = new bool[variables.Count];
            for (var variableIndex = 0; variableIndex < variables.Count; variableIndex++)
            {
                var bit = BitIndex.GetVariableBit(index, variableIndex, variables.Count);
                inputs[variableIndex] = bit;
                assignment[variables[variableIndex]] = bit;
            }

            var value = expression.Evaluate(variable => assignment[variable]);
            values[index] = value;
            rows.Add(new TruthTableRow(index, inputs, value));
        }

        return new TruthTable(variables, rows, values);
    }

    private static IReadOnlyList<char> CollectVariables(BoolExpr expression)
    {
        var variables = new HashSet<char>();
        expression.CollectVariables(variables);

        return variables.Count switch
        {
            0 => throw new ArgumentException("Expression must contain at least one variable."),
            > MaxVariableCount =>
                throw new ArgumentException($"Only up to {MaxVariableCount} variables are supported."),
            _ => variables.OrderBy(value => value).ToArray()
        };
    }
}

public static class CanonicalFormBuilder
{
    public static CanonicalForms Build(TruthTable table)
    {
        var sdnfIndices = new List<int>();
        var sknfIndices = new List<int>();
        var sdnfTerms = new List<string>();
        var sknfTerms = new List<string>();

        for (var index = 0; index < table.RowCount; index++)
        {
            var row = table.Rows[index];
            if (row.Value)
            {
                sdnfIndices.Add(row.Index);
                sdnfTerms.Add(BuildSdnfTerm(table.Variables, row.Inputs));
            }
            else
            {
                sknfIndices.Add(row.Index);
                sknfTerms.Add(BuildSknfTerm(table.Variables, row.Inputs));
            }
        }

        var sdnf = sdnfTerms.Count == 0 ? "0" : string.Join(" | ", sdnfTerms);
        var sknf = sknfTerms.Count == 0 ? "1" : string.Join(" & ", sknfTerms);
        var indexValue = BuildIndexValue(table.Values);
        return new CanonicalForms(sdnf, sknf, sdnfIndices, sknfIndices, indexValue, table.TruthVector);
    }

    public static string BuildSdnfFromValues(IReadOnlyList<char> variables, bool[] values)
    {
        var terms = new List<string>();
        for (var index = 0; index < values.Length; index++)
        {
            if (!values[index])
            {
                continue;
            }

            var literals = new List<string>(variables.Count);
            for (var variableIndex = 0; variableIndex < variables.Count; variableIndex++)
            {
                var isTrue = BitIndex.GetVariableBit(index, variableIndex, variables.Count);
                literals.Add(isTrue ? variables[variableIndex].ToString() : $"!{variables[variableIndex]}");
            }

            terms.Add(JoinConjunction(literals));
        }

        return terms.Count == 0 ? "0" : string.Join(" | ", terms);
    }

    private static string BuildSdnfTerm(IReadOnlyList<char> variables, IReadOnlyList<bool> inputs)
    {
        var literals = new List<string>(variables.Count);
        for (var i = 0; i < variables.Count; i++)
        {
            literals.Add(inputs[i] ? variables[i].ToString() : $"!{variables[i]}");
        }

        return JoinConjunction(literals);
    }

    private static string BuildSknfTerm(IReadOnlyList<char> variables, IReadOnlyList<bool> inputs)
    {
        var literals = new List<string>(variables.Count);
        literals.AddRange(variables.Select((t, i) => inputs[i] ? $"!{t}" : t.ToString()));

        return $"({string.Join(" | ", literals)})";
    }

    private static string JoinConjunction(IReadOnlyList<string> literals)
    {
        return literals.Count == 1 ? literals[0] : $"({string.Join(" & ", literals)})";
    }

    private static ulong BuildIndexValue(IReadOnlyList<bool> values)
    {
        ulong result = 0;
        for (var i = 0; i < values.Count; i++)
        {
            if (values[i])
            {
                result |= 1UL << i;
            }
        }

        return result;
    }
}

public static class PostClassAnalyzer
{
    public static PostClasses Analyze(TruthTable table)
    {
        var t0 = table.Values[0] == false;
        var t1 = table.Values[^1];
        var selfDual = IsSelfDual(table.Values);
        var monotone = IsMonotone(table.Values);
        var linear = IsLinear(table);
        return new PostClasses(t0, t1, selfDual, monotone, linear);
    }

    private static bool IsSelfDual(IReadOnlyList<bool> values)
    {
        for (var index = 0; index < values.Count; index++)
        {
            var opposite = values.Count - 1 - index;
            if (values[index] == values[opposite])
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsMonotone(IReadOnlyList<bool> values)
    {
        var rowCount = values.Count;
        for (var left = 0; left < rowCount; left++)
        {
            for (var right = 0; right < rowCount; right++)
            {
                if ((left & ~right) != 0)
                {
                    continue;
                }

                if (values[left] && !values[right])
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool IsLinear(TruthTable table)
    {
        var polynomial = ZhegalkinPolynomialBuilder.Build(table);
        return !polynomial.Coefficients.Where((t, mask) => t && BitOperations.PopCount((uint)mask) > 1).Any();
    }
}

public static class ZhegalkinPolynomialBuilder
{
    public static ZhegalkinPolynomial Build(TruthTable table)
    {
        var coefficients = BuildCoefficientVector(table.Values, table.VariableCount);
        var expression = BuildExpression(table.Variables, coefficients);
        return new ZhegalkinPolynomial(expression, coefficients);
    }

    private static bool[] BuildCoefficientVector(IReadOnlyList<bool> values, int variableCount)
    {
        var coefficients = values.Select(value => value ? 1 : 0).ToArray();
        var size = 1 << variableCount;

        for (var bit = 0; bit < variableCount; bit++)
        {
            var maskBit = 1 << bit;
            for (var mask = 0; mask < size; mask++)
            {
                if ((mask & maskBit) == 0)
                {
                    continue;
                }

                coefficients[mask] ^= coefficients[mask ^ maskBit];
            }
        }

        return coefficients.Select(value => value == 1).ToArray();
    }

    private static string BuildExpression(IReadOnlyList<char> variables, IReadOnlyList<bool> coefficients)
    {
        var terms = new List<string>();
        for (var mask = 0; mask < coefficients.Count; mask++)
        {
            if (!coefficients[mask])
            {
                continue;
            }

            terms.Add(BuildTerm(mask, variables));
        }

        return terms.Count == 0 ? "0" : string.Join(" ^ ", terms);
    }

    private static string BuildTerm(int mask, IReadOnlyList<char> variables)
    {
        if (mask == 0)
        {
            return "1";
        }

        var literals = new List<string>();
        for (var variableIndex = 0; variableIndex < variables.Count; variableIndex++)
        {
            var bitMask = 1 << (variables.Count - 1 - variableIndex);
            if ((mask & bitMask) != 0)
            {
                literals.Add(variables[variableIndex].ToString());
            }
        }

        return string.Join("*", literals);
    }
}

public static class FictiveVariableFinder
{
    public static IReadOnlyList<char> Find(TruthTable table)
    {
        var result = new List<char>();
        for (var variableIndex = 0; variableIndex < table.VariableCount; variableIndex++)
        {
            if (IsFictive(table.Values, variableIndex, table.VariableCount))
            {
                result.Add(table.Variables[variableIndex]);
            }
        }

        return result;
    }

    private static bool IsFictive(IReadOnlyList<bool> values, int variableIndex, int variableCount)
    {
        var variableMask = 1 << BitIndex.GetBitPosition(variableIndex, variableCount);
        for (var baseIndex = 0; baseIndex < values.Count; baseIndex++)
        {
            if ((baseIndex & variableMask) != 0)
            {
                continue;
            }

            var toggled = baseIndex | variableMask;
            if (values[baseIndex] != values[toggled])
            {
                return false;
            }
        }

        return true;
    }
}

public static class DerivativeAnalyzer
{
    private const int MaxDerivativeOrder = 4;

    public static IReadOnlyList<DerivativeResult> BuildAll(TruthTable table)
    {
        var result = new List<DerivativeResult>();
        var maxOrder = Math.Min(MaxDerivativeOrder, table.VariableCount);
        for (var order = 1; order <= maxOrder; order++)
        {
            foreach (var indices in BuildCombinations(table.VariableCount, order))
            {
                var names = indices.Select(index => table.Variables[index]).ToArray();
                var derivativeValues = CalculateDerivative(table, indices);
                var sdnf = CanonicalFormBuilder.BuildSdnfFromValues(table.Variables, derivativeValues);
                result.Add(new DerivativeResult(names, derivativeValues, sdnf));
            }
        }

        return result;
    }

    public static bool[] CalculateDerivative(TruthTable table, IReadOnlyList<int> variableIndices)
    {
        var current = table.Values.ToArray();
        foreach (var variableIndex in variableIndices)
        {
            var next = new bool[current.Length];
            var variableMask = 1 << BitIndex.GetBitPosition(variableIndex, table.VariableCount);

            for (var baseIndex = 0; baseIndex < current.Length; baseIndex++)
            {
                if ((baseIndex & variableMask) != 0)
                {
                    continue;
                }

                var toggled = baseIndex | variableMask;
                var derivativeValue = current[baseIndex] ^ current[toggled];
                next[baseIndex] = derivativeValue;
                next[toggled] = derivativeValue;
            }

            current = next;
        }

        return current;
    }

    private static IEnumerable<IReadOnlyList<int>> BuildCombinations(int count, int choose)
    {
        var buffer = new int[choose];
        return BuildCombinationsCore(0, 0);

        IEnumerable<IReadOnlyList<int>> BuildCombinationsCore(int start, int depth)
        {
            if (depth == choose)
            {
                yield return buffer.ToArray();
                yield break;
            }

            var remaining = choose - depth;
            for (var index = start; index <= count - remaining; index++)
            {
                buffer[depth] = index;
                foreach (var combination in BuildCombinationsCore(index + 1, depth + 1))
                {
                    yield return combination;
                }
            }
        }
    }
}

public static class BitIndex
{
    public static int GetBitPosition(int variableIndex, int variableCount) => variableCount - 1 - variableIndex;

    public static bool GetVariableBit(int rowIndex, int variableIndex, int variableCount) =>
        ((rowIndex >> GetBitPosition(variableIndex, variableCount)) & 1) == 1;
}
