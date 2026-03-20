using System.Diagnostics.CodeAnalysis;

namespace L2;

[ExcludeFromCodeCoverage]
public sealed class ConsoleRunner(TextReader? input = null, TextWriter? output = null)
{
    private readonly TextReader _input = input ?? Console.In;
    private readonly TextWriter _output = output ?? Console.Out;

    public int Run(string[]? args)
    {
        var resolvedArgs = args ?? [];
        if (resolvedArgs.Length > 0)
        {
            var expression = string.Join(" ", resolvedArgs);
            return RunSingle(expression);
        }

        var lastExitCode = 0;
        while (true)
        {
            _output.WriteLine("Enter boolean expression (or 'exit' to quit):");
            var expression = (_input.ReadLine() ?? string.Empty).Trim();
            if (expression.Length == 0)
            {
                _output.WriteLine("Expression is empty. Type 'exit' to quit.");
                continue;
            }

            if (expression.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                expression.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                return lastExitCode;
            }

            lastExitCode = RunSingle(expression);
        }
    }

    private int RunSingle(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            _output.WriteLine("Expression is empty.");
            return 1;
        }

        try
        {
            var parsed = BooleanExpressionParser.Parse(expression);
            var table = TruthTableBuilder.Build(parsed);
            var canonical = CanonicalFormBuilder.Build(table);
            var postClasses = PostClassAnalyzer.Analyze(table);
            var zhegalkin = ZhegalkinPolynomialBuilder.Build(table);
            var fictiveVariables = FictiveVariableFinder.Find(table);
            var derivatives = DerivativeAnalyzer.BuildAll(table);
            var calculation = BooleanMinimizer.MinimizeCalculation(table);
            var calculationTable = BooleanMinimizer.MinimizeCalculationTable(table);
            var karnaugh = BooleanMinimizer.MinimizeKarnaugh(table);

            PrintSection("Input");
            _output.WriteLine(expression);

            PrintSection("Truth Table");
            _output.WriteLine(ReportFormatter.FormatTruthTable(table));

            PrintSection("Canonical Forms");
            _output.WriteLine($"SDNF: {canonical.Sdnf}");
            _output.WriteLine($"SKNF: {canonical.Sknf}");

            PrintSection("Numeric Forms");
            _output.WriteLine(
                $"SDNF numeric: {ReportFormatter.FormatNumericForm("Sigma m", canonical.SdnfNumericForm)}");
            _output.WriteLine($"SKNF numeric: {ReportFormatter.FormatNumericForm("Pi M", canonical.SknfNumericForm)}");

            PrintSection("Index Form");
            _output.WriteLine($"Truth vector: {canonical.TruthVector}");
            _output.WriteLine($"Index: {canonical.IndexValue}");

            PrintSection("Post Classes");
            _output.WriteLine(ReportFormatter.FormatPostClasses(postClasses));

            PrintSection("Zhegalkin Polynomial");
            _output.WriteLine(zhegalkin.Expression);

            PrintSection("Fictive Variables");
            _output.WriteLine(fictiveVariables.Count == 0 ? "none" : string.Join(", ", fictiveVariables));

            PrintSection("Boolean Derivatives");
            foreach (var derivative in derivatives)
            {
                var name = ReportFormatter.FormatDerivativeName(derivative.ByVariables);
                var vector = ReportFormatter.FormatTruthVector(derivative.Values);
                _output.WriteLine($"{name}: {vector}, SDNF: {derivative.Sdnf}");
            }

            PrintSection("Minimization: Calculation");
            _output.WriteLine(MinimizationFormatter.FormatCalculation(calculation, table.Variables));

            PrintSection("Minimization: Calculation-Table");
            _output.WriteLine(MinimizationFormatter.FormatCalculation(calculationTable.BaseResult, table.Variables));
            _output.WriteLine();
            _output.WriteLine(MinimizationFormatter.FormatCoverageTable(calculationTable.Table, table.Variables));

            PrintSection("Minimization: Karnaugh");
            _output.WriteLine(MinimizationFormatter.FormatKarnaugh(karnaugh));
            return 0;
        }
        catch (Exception exception)
        {
            _output.WriteLine($"Error: {exception.Message}");
            return 1;
        }
    }

    private void PrintSection(string title)
    {
        _output.WriteLine();
        _output.WriteLine($"=== {title} ===");
    }
}
