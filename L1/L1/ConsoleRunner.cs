using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace L1;

[ExcludeFromCodeCoverage]
public static class ConsoleRunner
{
    public static void Run()
    {
        while (true)
        {
            PrintMenu();
            var choice = Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        ShowIntegerCodes();
                        break;
                    case "2":
                        AddInTwosComplement();
                        break;
                    case "3":
                        SubtractInTwosComplement();
                        break;
                    case "4":
                        MultiplyInSignMagnitude();
                        break;
                    case "5":
                        DivideInSignMagnitude();
                        break;
                    case "6":
                        FloatingPointOperation();
                        break;
                    case "7":
                        AddBcd();
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("Неизвестная команда.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            Console.WriteLine();
        }
    }

    private static void PrintMenu()
    {
        Console.WriteLine("Выберите операцию:");
        Console.WriteLine("1 - Перевод числа в прямой/обратный/дополнительный код");
        Console.WriteLine("2 - Сложение двух чисел в дополнительном коде");
        Console.WriteLine("3 - Вычитание через отрицание и сложение (доп. код)");
        Console.WriteLine("4 - Умножение двух чисел в прямом коде");
        Console.WriteLine("5 - Деление двух чисел в двоичную дробь (32 бита после точки)");
        Console.WriteLine("6 - Операции с плавающей точкой IEEE-754 (32 бита)");
        Console.WriteLine("7 - Сложение двух чисел в Gray BCD");
        Console.WriteLine("0 - Выход");
        Console.Write("Команда: ");
    }

    private static void ShowIntegerCodes()
    {
        var value = ReadInt("Введите целое число: ");

        var signMagnitude = BinaryIntegerMath.ToSignMagnitude(value);
        var onesComplement = BinaryIntegerMath.ToOnesComplement(value);
        var twosComplement = BinaryIntegerMath.ToTwosComplement(value);

        Console.WriteLine(
            $"Прямой код:      {BitHelpers.ToBitString(signMagnitude)} | {BinaryIntegerMath.FromSignMagnitude(signMagnitude)}");
        Console.WriteLine(
            $"Обратный код:    {BitHelpers.ToBitString(onesComplement)} | {BinaryIntegerMath.FromOnesComplement(onesComplement)}");
        Console.WriteLine(
            $"Дополнительный:  {BitHelpers.ToBitString(twosComplement)} | {BinaryIntegerMath.FromTwosComplement(twosComplement)}");
    }

    private static void AddInTwosComplement()
    {
        var left = ReadInt("Введите первое целое число: ");
        var right = ReadInt("Введите второе целое число: ");

        var result = BinaryIntegerMath.AddInTwosComplement(left, right);

        Console.WriteLine($"Результат (2):  {BitHelpers.ToBitString(result.Bits)}");
        Console.WriteLine($"Результат (10): {result.DecimalValue}");
    }

    private static void SubtractInTwosComplement()
    {
        var left = ReadInt("Введите уменьшаемое: ");
        var right = ReadInt("Введите вычитаемое: ");

        var result = BinaryIntegerMath.SubtractInTwosComplement(left, right);

        Console.WriteLine($"Результат (2):  {BitHelpers.ToBitString(result.Bits)}");
        Console.WriteLine($"Результат (10): {result.DecimalValue}");
    }

    private static void MultiplyInSignMagnitude()
    {
        var left = ReadInt("Введите первое целое число: ");
        var right = ReadInt("Введите второе целое число: ");

        var result = BinaryIntegerMath.MultiplyInSignMagnitude(left, right);

        Console.WriteLine($"Результат (2):  {BitHelpers.ToBitString(result.Bits)}");
        Console.WriteLine($"Результат (10): {result.DecimalValue}");
    }

    private static void DivideInSignMagnitude()
    {
        var left = ReadInt("Введите делимое: ");
        var right = ReadInt("Введите делитель: ");

        var result = BinaryIntegerMath.DivideInSignMagnitude(left, right);

        Console.WriteLine($"Результат (2):  {result.BinaryValue}");
        Console.WriteLine($"Результат (10): {result.DecimalValue.ToString(CultureInfo.InvariantCulture)}");
    }

    private static void FloatingPointOperation()
    {
        var left = ReadDouble("Введите первое число: ");
        var right = ReadDouble("Введите второе число: ");

        Console.Write("Операция (+, -, *, /): ");
        var operation = Console.ReadLine();

        var op = operation switch
        {
            "+" => FloatingOperation.Add,
            "-" => FloatingOperation.Subtract,
            "*" => FloatingOperation.Multiply,
            "/" => FloatingOperation.Divide,
            _ => throw new ArgumentException("Неизвестная операция.")
        };

        var result = Ieee754Math.Operate(left, right, op);

        Console.WriteLine($"Первое число (2):  {BitHelpers.ToBitString(result.LeftBits)}");
        Console.WriteLine($"Второе число (2):  {BitHelpers.ToBitString(result.RightBits)}");
        Console.WriteLine($"Результат (2):     {BitHelpers.ToBitString(result.ResultBits)}");
        Console.WriteLine($"Результат (10):    {result.DecimalValue.ToString(CultureInfo.InvariantCulture)}");
    }

    private static void AddBcd()
    {
        var left = ReadInt("Введите первое неотрицательное число: ");
        var right = ReadInt("Введите второе неотрицательное число: ");
        var result = BcdMath.Add(left, right);

        Console.WriteLine($"Результат (Gray BCD): {BitHelpers.ToBitString(result.Bits)}");
        Console.WriteLine($"Результат (10): {result.DecimalValue}");
    }

    private static int ReadInt(string prompt)
    {
        Console.Write(prompt);
        var input = Console.ReadLine();

        return !int.TryParse(input, out int value) ? throw new ArgumentException("Ожидалось целое число.") : value;
    }

    private static double ReadDouble(string prompt)
    {
        Console.Write(prompt);
        var input = Console.ReadLine();

        if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out double invariantValue))
        {
            return invariantValue;
        }

        return double.TryParse(input, NumberStyles.Float, CultureInfo.CurrentCulture, out double currentCultureValue)
            ? currentCultureValue
            : throw new ArgumentException("Ожидалось число с плавающей точкой.");
    }
}
