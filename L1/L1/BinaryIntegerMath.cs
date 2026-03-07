using System.Text;

namespace L1;

public sealed record IntegerOperationResult(int[] Bits, long DecimalValue);

public sealed record DivisionOperationResult(string BinaryValue, decimal DecimalValue, int FractionalBits);

public static class BinaryIntegerMath
{
    private const long MaxSignMagnitude = 2147483647;

    public static int[] ToSignMagnitude(int value)
    {
        return ToSignMagnitudeFromLong(value);
    }

    private static int[] ToSignMagnitudeFromLong(long value)
    {
        var isNegative = value < 0;
        var magnitude = isNegative ? -value : value;

        if (magnitude > MaxSignMagnitude)
        {
            throw new OverflowException("Value cannot be represented in 32-bit sign-magnitude format.");
        }

        var bits = new int[32];
        bits[0] = isNegative ? 1 : 0;
        WriteMagnitudeBits(bits, magnitude);
        return bits;
    }

    public static long FromSignMagnitude(int[] bits)
    {
        BitHelpers.Validate32Bits(bits, nameof(bits));

        var magnitude = ReadMagnitudeBits(bits);
        if (bits[0] == 1 && magnitude == 0)
        {
            return 0;
        }

        return bits[0] == 1 ? -magnitude : magnitude;
    }

    public static int[] ToOnesComplement(int value)
    {
        switch (value)
        {
            case >= 0:
                return ToSignMagnitude(value);
            case int.MinValue:
                throw new OverflowException(
                    "Minimum Int32 value cannot be represented in ones' complement with this implementation.");
            default:
            {
                var positive = ToSignMagnitude(-value);
                return Invert(positive);
            }
        }
    }

    public static long FromOnesComplement(int[] bits)
    {
        BitHelpers.Validate32Bits(bits, nameof(bits));

        if (bits[0] == 0)
        {
            return FromSignMagnitude(bits);
        }

        var positive = Invert(bits);
        var magnitude = ReadMagnitudeBits(positive);

        if (magnitude == 0)
        {
            return 0;
        }

        return -magnitude;
    }

    public static int[] ToTwosComplement(int value)
    {
        switch (value)
        {
            case int.MinValue:
            {
                var minValueBits = new int[32];
                minValueBits[0] = 1;
                return minValueBits;
            }
            case >= 0:
                return ToSignMagnitude(value);
            default:
            {
                var ones = ToOnesComplement(value);
                return AddOne(ones);
            }
        }
    }

    public static long FromTwosComplement(int[] bits)
    {
        BitHelpers.Validate32Bits(bits, nameof(bits));

        if (bits[0] == 0)
        {
            return bits.Aggregate<int, long>(0, (current, t) => current * 2 + t);
        }

        var positive = AddOne(Invert(bits));
        var magnitude = positive.Aggregate<int, long>(0, (current, t) => current * 2 + t);

        return -magnitude;
    }

    public static int[] AddTwosComplement(int[] left, int[] right)
    {
        BitHelpers.Validate32Bits(left, nameof(left));
        BitHelpers.Validate32Bits(right, nameof(right));

        var result = new int[32];
        var carry = 0;

        for (var i = 31; i >= 0; i--)
        {
            var sum = left[i] + right[i] + carry;
            result[i] = sum % 2;
            carry = sum / 2;
        }

        return result;
    }

    public static IntegerOperationResult AddInTwosComplement(int left, int right)
    {
        var leftBits = ToTwosComplement(left);
        var rightBits = ToTwosComplement(right);
        var sumBits = AddTwosComplement(leftBits, rightBits);

        return new IntegerOperationResult(sumBits, FromTwosComplement(sumBits));
    }

    public static int[] NegateTwosComplement(int[] bits)
    {
        BitHelpers.Validate32Bits(bits, nameof(bits));
        return AddOne(Invert(bits));
    }

    public static IntegerOperationResult SubtractInTwosComplement(int left, int right)
    {
        var leftBits = ToTwosComplement(left);
        var rightBits = ToTwosComplement(right);
        var negativeRight = NegateTwosComplement(rightBits);
        var resultBits = AddTwosComplement(leftBits, negativeRight);

        return new IntegerOperationResult(resultBits, FromTwosComplement(resultBits));
    }

    public static IntegerOperationResult MultiplyInSignMagnitude(int left, int right)
    {
        if (left == int.MinValue || right == int.MinValue)
        {
            throw new OverflowException("Minimum Int32 value is not supported for sign-magnitude multiplication.");
        }

        var a = left < 0 ? -(long)left : left;
        var b = right < 0 ? -(long)right : right;
        long product = 0;

        while (b > 0)
        {
            if (b % 2 == 1)
            {
                product += a;
            }

            a *= 2;
            b /= 2;
        }

        var isNegative = (left < 0) ^ (right < 0);
        if (isNegative && product != 0)
        {
            product = -product;
        }

        var bits = ToSignMagnitudeFromLong(product);
        return new IntegerOperationResult(bits, product);
    }

    public static DivisionOperationResult DivideInSignMagnitude(int dividend, int divisor, int fractionalBits = 32)
    {
        if (divisor == 0)
        {
            throw new DivideByZeroException("Division by zero is not allowed.");
        }

        if (fractionalBits is < 0 or > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(fractionalBits), "Fractional bit count must be in range [0..64].");
        }

        var decimalValue = dividend / (decimal)divisor;
        var binaryValue = ToBinaryQuotient(dividend, divisor, fractionalBits);

        return new DivisionOperationResult(binaryValue, decimalValue, fractionalBits);
    }

    private static void WriteMagnitudeBits(int[] bits, long magnitude)
    {
        for (var i = 31; i >= 1; i--)
        {
            bits[i] = (int)(magnitude % 2);
            magnitude /= 2;
        }
    }

    private static long ReadMagnitudeBits(int[] bits)
    {
        long value = 0;
        for (var i = 1; i < bits.Length; i++)
        {
            value = value * 2 + bits[i];
        }

        return value;
    }

    private static int[] AddOne(int[] bits)
    {
        var result = BitHelpers.CloneBits(bits);

        for (var i = result.Length - 1; i >= 0; i--)
        {
            if (result[i] == 0)
            {
                result[i] = 1;
                return result;
            }

            result[i] = 0;
        }

        return result;
    }

    private static int[] Invert(int[] bits)
    {
        var result = new int[bits.Length];

        for (var i = 0; i < bits.Length; i++)
        {
            result[i] = bits[i] == 0 ? 1 : 0;
        }

        return result;
    }

    private static string ToBinaryQuotient(int dividend, int divisor, int fractionalBits)
    {
        var absDividend = dividend < 0 ? -(long)dividend : dividend;
        var absDivisor = divisor < 0 ? -(long)divisor : divisor;
        var integerPart = absDividend / absDivisor;
        var remainder = absDividend % absDivisor;
        var isNegative = (dividend < 0) ^ (divisor < 0);
        var builder = new StringBuilder();

        if (isNegative && (integerPart != 0 || remainder != 0))
        {
            builder.Append('-');
        }

        builder.Append(ToBinaryMagnitude(integerPart));

        if (fractionalBits == 0)
        {
            return builder.ToString();
        }

        builder.Append('.');

        for (var i = 0; i < fractionalBits; i++)
        {
            remainder *= 2;

            if (remainder >= absDivisor)
            {
                builder.Append('1');
                remainder -= absDivisor;
            }
            else
            {
                builder.Append('0');
            }
        }

        return builder.ToString();
    }

    private static string ToBinaryMagnitude(long value)
    {
        if (value == 0)
        {
            return "0";
        }

        var buffer = new char[64];
        var position = buffer.Length;
        var current = value;

        while (current > 0)
        {
            buffer[--position] = current % 2 == 0 ? '0' : '1';
            current /= 2;
        }

        return new string(buffer, position, buffer.Length - position);
    }
}
