namespace L1;

public enum FloatingOperation
{
    Add,
    Subtract,
    Multiply,
    Divide
}

public sealed record FloatingOperationResult(int[] LeftBits, int[] RightBits, int[] ResultBits, double DecimalValue);

public static class Ieee754Math
{
    public static int[] ToIeee754(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentException("NaN and Infinity are not supported by this simplified implementation.", nameof(value));
        }

        var bits = new int[32];

        switch (value)
        {
            case 0d:
                return bits;
            case < 0:
                bits[0] = 1;
                value = -value;
                break;
        }

        var exponent = 0;
        while (value >= 2.0)
        {
            value /= 2.0;
            exponent++;
        }

        while (value < 1.0)
        {
            value *= 2.0;
            exponent--;
        }

        switch (exponent)
        {
            case > 127:
                throw new OverflowException("Value is too large for IEEE-754 binary32.");
            case >= -126:
            {
                int biasedExponent = exponent + 127;
                WriteByte(bits, 1, biasedExponent);
                FillMantissa(bits, value - 1.0);
                return bits;
            }
        }

        var shift = -126 - exponent;
        var subnormalFraction = value;

        for (int i = 0; i < shift; i++)
        {
            subnormalFraction /= 2.0;
        }

        FillMantissa(bits, subnormalFraction);
        return bits;
    }

    public static double FromIeee754(int[] bits)
    {
        BitHelpers.Validate32Bits(bits, nameof(bits));

        var exponent = ReadByte(bits, 1);
        if (exponent == 255)
        {
            throw new ArgumentException("NaN/Infinity payload is not supported by this simplified implementation.", nameof(bits));
        }

        var mantissa = ReadMantissa(bits);
        double significand;
        int actualExponent;

        if (exponent == 0)
        {
            significand = mantissa;
            actualExponent = -126;
        }
        else
        {
            significand = 1.0 + mantissa;
            actualExponent = exponent - 127;
        }

        var value = significand;

        switch (actualExponent)
        {
            case > 0:
            {
                for (int i = 0; i < actualExponent; i++)
                {
                    value *= 2.0;
                }

                break;
            }
            case < 0:
            {
                for (int i = 0; i < -actualExponent; i++)
                {
                    value /= 2.0;
                }

                break;
            }
        }

        if (bits[0] == 1)
        {
            value = -value;
        }

        return value;
    }

    public static FloatingOperationResult Operate(double left, double right, FloatingOperation operation)
    {
        var leftBits = ToIeee754(left);
        var rightBits = ToIeee754(right);

        var leftValue = FromIeee754(leftBits);
        var rightValue = FromIeee754(rightBits);

        var result = operation switch
        {
            FloatingOperation.Add => leftValue + rightValue,
            FloatingOperation.Subtract => leftValue - rightValue,
            FloatingOperation.Multiply => leftValue * rightValue,
            FloatingOperation.Divide => rightValue == 0
                ? throw new DivideByZeroException("Division by zero is not allowed.")
                : leftValue / rightValue,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unsupported operation")
        };

        var resultBits = ToIeee754(result);
        var decimalValue = FromIeee754(resultBits);

        return new FloatingOperationResult(leftBits, rightBits, resultBits, decimalValue);
    }

    private static void WriteByte(int[] bits, int startIndex, int value)
    {
        for (var i = startIndex + 7; i >= startIndex; i--)
        {
            bits[i] = value % 2;
            value /= 2;
        }
    }

    private static int ReadByte(int[] bits, int startIndex)
    {
        var value = 0;

        for (var i = startIndex; i < startIndex + 8; i++)
        {
            value = value * 2 + bits[i];
        }

        return value;
    }

    private static void FillMantissa(int[] bits, double fraction)
    {
        var value = fraction;

        for (var i = 9; i < bits.Length; i++)
        {
            value *= 2.0;

            if (!(value >= 1.0)) continue;
            bits[i] = 1;
            value -= 1.0;
        }
    }

    private static double ReadMantissa(int[] bits)
    {
        double value = 0;
        var weight = 0.5;

        for (var i = 9; i < bits.Length; i++)
        {
            if (bits[i] == 1)
            {
                value += weight;
            }

            weight /= 2.0;
        }

        return value;
    }
}
