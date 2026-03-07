namespace L1;

public sealed record BcdOperationResult(int[] Bits, int DecimalValue);

public static class BcdMath
{
    private const int BitsPerDigit = 4;
    private const int MaxDigits = 8;

    private static readonly int[][] GrayDigitTable =
    [
        [0, 0, 0, 0], // 0
        [0, 0, 0, 1], // 1
        [0, 0, 1, 1], // 2
        [0, 0, 1, 0], // 3
        [0, 1, 1, 0], // 4
        [0, 1, 1, 1], // 5
        [0, 1, 0, 1], // 6
        [0, 1, 0, 0], // 7
        [1, 1, 0, 0], // 8
        [1, 1, 0, 1] // 9
    ];

    public static int[] EncodeTo32Bits(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Gray BCD supports only non-negative values.");
        }

        var digits = ExtractDigits(value);
        if (digits.Count > MaxDigits)
        {
            throw new OverflowException("Value does not fit into 32 bits of packed Gray BCD.");
        }

        var bits = new int[32];
        var offset = bits.Length - BitsPerDigit;
        for (var i = digits.Count - 1; i >= 0; i--)
        {
            WriteDigit(bits, offset, digits[i]);
            offset -= BitsPerDigit;
        }

        return bits;
    }

    public static BcdOperationResult Add(int left, int right)
    {
        if (left < 0 || right < 0)
        {
            throw new ArgumentOutOfRangeException("Gray BCD supports only non-negative values.");
        }

        var leftBits = EncodeTo32Bits(left);
        var rightBits = EncodeTo32Bits(right);
        var resultBits = new int[32];
        var carry = 0;

        for (var offset = resultBits.Length - BitsPerDigit; offset >= 0; offset -= BitsPerDigit)
        {
            var sum = DecodeDigit(leftBits, offset) + DecodeDigit(rightBits, offset) + carry;
            carry = sum / 10;
            WriteDigit(resultBits, offset, sum % 10);
        }

        if (carry != 0)
        {
            throw new OverflowException("Sum does not fit into 32 bits of packed Gray BCD.");
        }

        return new BcdOperationResult(resultBits, DecodeToDecimal(resultBits));
    }

    private static List<int> ExtractDigits(int value)
    {
        if (value == 0)
        {
            return [0];
        }

        var reversed = new List<int>();
        int current = value;

        while (current > 0)
        {
            reversed.Add(current % 10);
            current /= 10;
        }

        reversed.Reverse();
        return reversed;
    }

    private static int DecodeToDecimal(int[] bits)
    {
        var value = 0;

        for (var offset = 0; offset < bits.Length; offset += BitsPerDigit)
        {
            value = checked(value * 10 + DecodeDigit(bits, offset));
        }

        return value;
    }

    private static int DecodeDigit(int[] bits, int offset)
    {
        for (var digit = 0; digit < GrayDigitTable.Length; digit++)
        {
            var code = GrayDigitTable[digit];
            if (bits[offset] == code[0] &&
                bits[offset + 1] == code[1] &&
                bits[offset + 2] == code[2] &&
                bits[offset + 3] == code[3])
            {
                return digit;
            }
        }

        throw new ArgumentException("Bits contain an invalid Gray BCD digit.", nameof(bits));
    }

    private static void WriteDigit(int[] bits, int offset, int digit)
    {
        if (digit < 0 || digit > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(digit), "Digit must be in range [0..9].");
        }

        var code = GrayDigitTable[digit];
        bits[offset] = code[0];
        bits[offset + 1] = code[1];
        bits[offset + 2] = code[2];
        bits[offset + 3] = code[3];
    }
}
