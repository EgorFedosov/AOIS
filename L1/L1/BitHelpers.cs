using System.Text;

namespace L1;

public static class BitHelpers
{
    public static void Validate32Bits(int[] bits, string paramName)
    {
        ValidateBits(bits, 32, paramName);
    }

    private static void ValidateBits(int[] bits, int expectedLength, string paramName)
    {
        ArgumentNullException.ThrowIfNull(bits, paramName);

        if (bits.Length != expectedLength)
        {
            throw new ArgumentException($"Array must have length {expectedLength}.", paramName);
        }

        if (bits.Any(t => t != 0 && t != 1))
        {
            throw new ArgumentException("Array can contain only 0 or 1.", paramName);
        }
    }

    public static int[] CloneBits(int[] bits)
    {
        ArgumentNullException.ThrowIfNull(bits);
        return (int[])bits.Clone();
    }

    public static string ToBitString(int[] bits, bool groupByFour = true)
    {
        ArgumentNullException.ThrowIfNull(bits);

        var builder = new StringBuilder(bits.Length + bits.Length / 4);
        for (var i = 0; i < bits.Length; i++)
        {
            builder.Append(bits[i]);

            if (groupByFour && i != bits.Length - 1 && (i + 1) % 4 == 0)
            {
                builder.Append(' ');
            }
        }

        return builder.ToString();
    }

    public static int[] ToBits(int value, int bitCount)
    {
        if (bitCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bitCount), "Bit count must be positive.");
        }

        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be non-negative.");
        }

        var bits = new int[bitCount];
        var current = value;

        for (var i = bitCount - 1; i >= 0; i--)
        {
            bits[i] = current % 2;
            current /= 2;
        }

        return current != 0 ? throw new OverflowException("Value does not fit in the requested bit count.") : bits;
    }

    public static int BitsToInt(ReadOnlySpan<int> bits)
    {
        var value = 0;

        foreach (var t in bits)
        {
            if (t != 0 && t != 1)
            {
                throw new ArgumentException("Span can contain only 0 or 1.", nameof(bits));
            }

            value = value * 2 + t;
        }

        return value;
    }
}