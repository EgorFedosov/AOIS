using L2;
using Xunit;
using System.Reflection;

namespace TestsL2;

public class UnitTest1
{
    [Fact]
    public void Parser_RespectsOperatorPrecedence()
    {
        var expression = BooleanExpressionParser.Parse("!a & b | c");

        var value1 = Evaluate(expression, ('a', false), ('b', true), ('c', false));
        var value2 = Evaluate(expression, ('a', false), ('b', false), ('c', false));

        Assert.True(value1);
        Assert.False(value2);
    }
    [Fact]
    public void Parser_SupportsUnicodeOperators()
    {
        var unicode = BooleanExpressionParser.Parse("¬(a∧b)∨c");
        var mojibake = BooleanExpressionParser.Parse("В¬(aв€§b)в€Ёc");
        var ascii = BooleanExpressionParser.Parse("!(a&b)|c");
        var unicodeImplies = BooleanExpressionParser.Parse("a→b");
        var asciiImplies = BooleanExpressionParser.Parse("a->b");
        var unicodeEquivalent = BooleanExpressionParser.Parse("a↔b");
        var asciiEquivalent = BooleanExpressionParser.Parse("a~b");

        var unicodeTable = TruthTableBuilder.Build(unicode);
        var mojibakeTable = TruthTableBuilder.Build(mojibake);
        var asciiTable = TruthTableBuilder.Build(ascii);

        Assert.Equal(asciiTable.TruthVector, unicodeTable.TruthVector);
        Assert.Equal(asciiTable.TruthVector, mojibakeTable.TruthVector);
        Assert.Equal(
            Evaluate(asciiImplies, ('a', true), ('b', false)),
            Evaluate(unicodeImplies, ('a', true), ('b', false)));
        Assert.Equal(
            Evaluate(asciiEquivalent, ('a', true), ('b', false)),
            Evaluate(unicodeEquivalent, ('a', true), ('b', false)));
    }

    [Fact]
    public void Parser_SupportsEquivalentAndRightAssociativeImplication()
    {
        var equivalent = BooleanExpressionParser.Parse("a~b");
        var implies = BooleanExpressionParser.Parse("a->b->c");

        Assert.True(Evaluate(equivalent, ('a', true), ('b', true)));
        Assert.False(Evaluate(equivalent, ('a', true), ('b', false)));
        Assert.True(Evaluate(implies, ('a', false), ('b', true), ('c', false)));
    }

    [Fact]
    public void Parser_ThrowsOnInvalidInput()
    {
        Assert.Throws<ArgumentException>(() => BooleanExpressionParser.Parse("a#b"));
        Assert.Throws<ArgumentException>(() => BooleanExpressionParser.Parse("(a|b"));
        Assert.Throws<ArgumentException>(() => BooleanExpressionParser.Parse("f"));
        Assert.Throws<ArgumentException>(() => BooleanExpressionParser.Parse(string.Empty));
    }

    [Fact]
    public void TruthTable_BuildsOrderedVariablesAndRows()
    {
        var table = BuildTable("c | a & b");

        Assert.Equal(['a', 'b', 'c'], table.Variables);
        Assert.Equal(8, table.RowCount);
        Assert.Equal("01010111", table.TruthVector);
    }

    [Fact]
    public void TruthTable_ValuesAreReadOnly()
    {
        var table = BuildTable("a|b");

        Assert.False(table.Values is bool[]);
        var readOnlyValues = Assert.IsAssignableFrom<IList<bool>>(table.Values);
        Assert.Throws<NotSupportedException>(() => readOnlyValues[0] = !readOnlyValues[0]);
    }

    [Fact]
    public void TruthTable_ThrowsForInvalidVariableCount()
    {
        Assert.Throws<ArgumentException>(() => TruthTableBuilder.Build(new EmptyExpression()));
        Assert.Throws<ArgumentException>(() => TruthTableBuilder.Build(new TooManyVariablesExpression()));
    }

    [Fact]
    public void CanonicalForms_AreComputedCorrectly()
    {
        var table = BuildTable("!(!a->!b)|c");
        var forms = CanonicalFormBuilder.Build(table);

        Assert.Equal("(!a & !b & c) | (!a & b & !c) | (!a & b & c) | (a & !b & c) | (a & b & c)", forms.Sdnf);
        Assert.Equal("(a | b | c) & (!a | b | c) & (!a | !b | c)", forms.Sknf);
        Assert.Equal([1, 2, 3, 5, 7], forms.SdnfNumericForm);
        Assert.Equal([0, 4, 6], forms.SknfNumericForm);
        Assert.Equal("01110101", forms.TruthVector);
        Assert.Equal(174UL, forms.IndexValue);
    }

    [Fact]
    public void CanonicalForms_BuildSdnfFromValuesHandlesZeroAndOneCases()
    {
        var sdnfZero = CanonicalFormBuilder.BuildSdnfFromValues(['a'], [false, false]);
        var sdnfOne = CanonicalFormBuilder.BuildSdnfFromValues(['a'], [false, true]);

        Assert.Equal("0", sdnfZero);
        Assert.Equal("a", sdnfOne);
    }

    [Fact]
    public void PostClasses_AreDetectedCorrectly()
    {
        var andPost = PostClassAnalyzer.Analyze(BuildTable("a&b"));
        var xorPost = PostClassAnalyzer.Analyze(BuildTable("(!a&b)|(a&!b)"));
        var identityPost = PostClassAnalyzer.Analyze(BuildTable("a"));

        Assert.True(andPost.T0);
        Assert.True(andPost.T1);
        Assert.False(andPost.S);
        Assert.True(andPost.M);
        Assert.False(andPost.L);

        Assert.True(xorPost.L);
        Assert.False(xorPost.M);

        Assert.True(identityPost.S);
    }

    [Fact]
    public void ZhegalkinPolynomial_IsBuiltCorrectly()
    {
        var andPolynomial = ZhegalkinPolynomialBuilder.Build(BuildTable("a&b"));
        var xorPolynomial = ZhegalkinPolynomialBuilder.Build(BuildTable("(!a&b)|(a&!b)"));

        Assert.Equal("a*b", andPolynomial.Expression);
        Assert.False(andPolynomial.Coefficients[0]);
        Assert.False(andPolynomial.Coefficients[1]);
        Assert.False(andPolynomial.Coefficients[2]);
        Assert.True(andPolynomial.Coefficients[3]);

        Assert.False(xorPolynomial.Coefficients[0]);
        Assert.True(xorPolynomial.Coefficients[1]);
        Assert.True(xorPolynomial.Coefficients[2]);
        Assert.False(xorPolynomial.Coefficients[3]);
    }

    [Fact]
    public void FictiveVariables_AreFound()
    {
        var table = BuildTable("a&(b|!b)");
        var fictive = FictiveVariableFinder.Find(table);

        Assert.Single(fictive);
        Assert.Equal('b', fictive[0]);
    }

    [Fact]
    public void Derivatives_AreComputedForPartialAndMixed()
    {
        var table = BuildTable("a&b");
        var derivativeByA = DerivativeAnalyzer.CalculateDerivative(table, [0]);
        var derivativeByAb = DerivativeAnalyzer.CalculateDerivative(table, [0, 1]);

        Assert.Equal("0101", ToVector(derivativeByA));
        Assert.Equal("1111", ToVector(derivativeByAb));
    }

    [Fact]
    public void Derivatives_BuildAllProducesOrdersFromOneToFour()
    {
        var table = BuildTable("a&b&c&d&e");
        var all = DerivativeAnalyzer.BuildAll(table);

        Assert.Equal(30, all.Count);
        Assert.All(all, derivative => Assert.InRange(derivative.ByVariables.Count, 1, 4));
    }

    [Fact]
    public void Implicant_CombineAndFormattingWork()
    {
        var first = new Implicant(2, 0, [2]);
        var second = new Implicant(3, 0, [3]);

        Assert.True(first.CanCombineWith(second, out var differenceBit));
        var combined = first.CombineWith(second, differenceBit);

        Assert.Equal("01X", combined.ToPattern(3));
        Assert.Equal("(!a & b)", combined.ToTerm(['a', 'b', 'c']));
        Assert.True(combined.Covers(2));
        Assert.True(combined.Covers(3));
        Assert.False(combined.Covers(1));

        var duplicate = new Implicant(2, 0, [2]);
        Assert.Equal(first, duplicate);
    }

    [Fact]
    public void BitIndex_ReturnsExpectedBits()
    {
        Assert.Equal(2, BitIndex.GetBitPosition(0, 3));
        Assert.Equal(1, BitIndex.GetBitPosition(1, 3));
        Assert.Equal(0, BitIndex.GetBitPosition(2, 3));

        Assert.True(BitIndex.GetVariableBit(6, 0, 3));
        Assert.True(BitIndex.GetVariableBit(6, 1, 3));
        Assert.False(BitIndex.GetVariableBit(6, 2, 3));
    }

    [Fact]
    public void MinimizationCalculation_FindsExpectedMinimalTerms()
    {
        var table = BuildTable("!(!a->!b)|c");
        var result = BooleanMinimizer.MinimizeCalculation(table);
        var terms = result.SelectedImplicants.Select(implicant => implicant.ToTerm(table.Variables)).ToHashSet();

        Assert.Equal([1, 2, 3, 5, 7], result.Minterms);
        Assert.Equal(2, result.SelectedImplicants.Count);
        Assert.Contains("c", terms);
        Assert.Contains("(!a & b)", terms);
        Assert.True(result.Stages.Count >= 2);
        Assert.Contains(result.Stages, stage => stage.PairCombinations.Count > 0);
    }

    [Fact]
    public void MinimizationCalculation_HandlesConstantFunctions()
    {
        var zeroResult = BooleanMinimizer.MinimizeCalculation(BuildTable("a&!a"));
        var oneResult = BooleanMinimizer.MinimizeCalculation(BuildTable("a|!a"));

        Assert.Equal("0", zeroResult.MinimalDnf);
        Assert.Empty(zeroResult.SelectedImplicants);

        Assert.Equal("1", oneResult.MinimalDnf);
        Assert.Single(oneResult.SelectedImplicants);
        Assert.Equal("1", oneResult.SelectedImplicants[0].ToTerm(['a']));
    }

    [Fact]
    public void MinimizationTable_BuildsCoverageMatrix()
    {
        var table = BuildTable("!(!a->!b)|c");
        var result = BooleanMinimizer.MinimizeCalculationTable(table);

        Assert.Equal(result.BaseResult.PrimeImplicants.Count, result.Table.Matrix.GetLength(0));
        Assert.Equal(result.BaseResult.Minterms.Count, result.Table.Matrix.GetLength(1));

        for (var column = 0; column < result.Table.Minterms.Count; column++)
        {
            var covered = false;
            for (var row = 0; row < result.Table.Implicants.Count; row++)
            {
                covered |= result.Table.Matrix[row, column];
            }

            Assert.True(covered);
        }
    }

    [Fact]
    public void KarnaughMap_BuildsForThreeVariables()
    {
        var map = BooleanMinimizer.MinimizeKarnaugh(BuildTable("!(!a->!b)|c"));

        Assert.Null(map.Note);
        Assert.Equal(2, map.RowLabels.Count);
        Assert.Equal(4, map.ColumnLabels.Count);
        Assert.NotEmpty(map.Groups);
        Assert.Equal(2, map.Values.GetLength(0));
        Assert.Equal(4, map.Values.GetLength(1));
    }

    [Fact]
    public void KarnaughMap_ReturnsNoteForFiveVariables()
    {
        var map = BooleanMinimizer.MinimizeKarnaugh(BuildTable("a|b|c|d|e"));

        Assert.NotNull(map.Note);
        Assert.Empty(map.RowLabels);
        Assert.Empty(map.ColumnLabels);
        Assert.Empty(map.Values);
    }

    [Fact]
    public void KarnaughMap_CoversSplitsForOneTwoAndFourVariables()
    {
        var map1 = BooleanMinimizer.MinimizeKarnaugh(BuildTable("a"));
        var map2 = BooleanMinimizer.MinimizeKarnaugh(BuildTable("a|b"));
        var map4 = BooleanMinimizer.MinimizeKarnaugh(BuildTable("(a&b)|(c&d)"));

        Assert.Equal(1, map1.Values.GetLength(0));
        Assert.Equal(2, map1.Values.GetLength(1));
        Assert.Equal(2, map2.Values.GetLength(0));
        Assert.Equal(2, map2.Values.GetLength(1));
        Assert.Equal(4, map4.Values.GetLength(0));
        Assert.Equal(4, map4.Values.GetLength(1));
    }

    [Fact]
    public void Minimizer_PrivateHelpers_AreCoveredViaReflection()
    {
        var minimizerType = typeof(BooleanMinimizer);
        var findFirstSetBit = minimizerType.GetMethod("FindFirstSetBit", BindingFlags.NonPublic | BindingFlags.Static);
        var buildGrayCodes = minimizerType.GetMethod("BuildGrayCodes", BindingFlags.NonPublic | BindingFlags.Static);
        var toBinaryLabel = minimizerType.GetMethod("ToBinaryLabel", BindingFlags.NonPublic | BindingFlags.Static);
        var selectMinimalCover = minimizerType.GetMethod("SelectMinimalCover", BindingFlags.NonPublic | BindingFlags.Static);
        var searchBestCover = minimizerType.GetMethod("SearchBestCover", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(findFirstSetBit);
        Assert.NotNull(buildGrayCodes);
        Assert.NotNull(toBinaryLabel);
        Assert.NotNull(selectMinimalCover);
        Assert.NotNull(searchBestCover);

        Assert.Equal(-1, (int)findFirstSetBit.Invoke(null, [0UL])!);
        Assert.Equal(3, (int)findFirstSetBit.Invoke(null, [8UL])!);

        var gray0 = (IReadOnlyList<int>)buildGrayCodes.Invoke(null, [0])!;
        var gray2 = (IReadOnlyList<int>)buildGrayCodes.Invoke(null, [2])!;
        Assert.Equal([0], gray0);
        Assert.Equal([0, 1, 3, 2], gray2);

        Assert.Equal("0", (string)toBinaryLabel.Invoke(null, [0, 0])!);
        Assert.Equal("101", (string)toBinaryLabel.Invoke(null, [5, 3])!);

        var primeImplicants = new List<Implicant>
        {
            new(1, 6, [1, 3, 5, 7]),
            new(2, 5, [2, 3, 6, 7]),
            new(4, 3, [4, 5, 6, 7])
        };
        var selected = (IReadOnlyList<Implicant>)selectMinimalCover.Invoke(null, [primeImplicants, new List<int> { 3, 5, 7 }, 3])!;
        Assert.Single(selected);
        Assert.Equal("c", selected[0].ToTerm(['a', 'b', 'c']));

        var emptyOptions = new Dictionary<int, IReadOnlyList<int>> { [0] = [] };
        var failedSearch = (IReadOnlyList<int>)searchBestCover.Invoke(
            null,
            [new List<int> { 0 }, emptyOptions, new List<ulong> { 0UL }, 1UL, primeImplicants, 3])!;
        Assert.Empty(failedSearch);
    }

    private static TruthTable BuildTable(string expression)
    {
        var parsed = BooleanExpressionParser.Parse(expression);
        return TruthTableBuilder.Build(parsed);
    }

    private static bool Evaluate(BoolExpr expression, params (char Name, bool Value)[] assignment)
    {
        var map = assignment.ToDictionary(pair => pair.Name, pair => pair.Value);
        return expression.Evaluate(variable => map[variable]);
    }

    private static string ToVector(IEnumerable<bool> values) =>
        new(values.Select(value => value ? '1' : '0').ToArray());

    private sealed class EmptyExpression : BoolExpr
    {
        public override bool Evaluate(Func<char, bool> variableResolver) => false;

        public override void CollectVariables(ISet<char> variables)
        {
        }
    }

    private sealed class TooManyVariablesExpression : BoolExpr
    {
        public override bool Evaluate(Func<char, bool> variableResolver) => false;

        public override void CollectVariables(ISet<char> variables)
        {
            foreach (var variable in new[] { 'a', 'b', 'c', 'd', 'e', 'f' })
            {
                variables.Add(variable);
            }
        }
    }
}

