using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace L2;

[ExcludeFromCodeCoverage]
public static class Program
{
    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        var runner = new ConsoleRunner(Console.In, Console.Out);
        Environment.ExitCode = runner.Run(args);
    }
}
