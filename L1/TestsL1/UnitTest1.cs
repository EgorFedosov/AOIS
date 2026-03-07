using L1;

namespace TestProject1;

public class UnitTest1
{
    [Fact]
    public void ToSignMagnitude_PositiveAndNegative_Works()
    {
        var positive = BinaryIntegerMath.ToSignMagnitude(10);
        var negative = BinaryIntegerMath.ToSignMagnitude(-10);

        Assert.Equal("00000000000000000000000000001010", BitHelpers.ToBitString(positive, false));
        Assert.Equal("10000000000000000000000000001010", BitHelpers.ToBitString(negative, false));
        Assert.Equal(10, BinaryIntegerMath.FromSignMagnitude(positive));
        Assert.Equal(-10, BinaryIntegerMath.FromSignMagnitude(negative));
    }

    [Fact]
    public void ToSignMagnitude_IntMin_Throws()
    {
        Assert.Throws<OverflowException>(() => BinaryIntegerMath.ToSignMagnitude(int.MinValue));
    }

    [Fact]
    public void ToOnesComplement_ForNegative_Works()
    {
        int[] ones = BinaryIntegerMath.ToOnesComplement(-5);

        Assert.Equal("11111111111111111111111111111010", BitHelpers.ToBitString(ones, false));
        Assert.Equal(-5, BinaryIntegerMath.FromOnesComplement(ones));
    }

    [Fact]
    public void ToOnesComplement_IntMin_Throws()
    {
        Assert.Throws<OverflowException>(() => BinaryIntegerMath.ToOnesComplement(int.MinValue));
    }

    [Theory]
    [InlineData(-1000)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1000)]
    [InlineData(int.MinValue)]
    public void TwosComplement_RoundTrip_Works(int value)
    {
        int[] bits = BinaryIntegerMath.ToTwosComplement(value);
        long decoded = BinaryIntegerMath.FromTwosComplement(bits);

        Assert.Equal(value, decoded);
    }

    [Fact]
    public void AddTwosComplement_DirectAndHighLevel_Works()
    {
        var a = BinaryIntegerMath.ToTwosComplement(3);
        var b = BinaryIntegerMath.ToTwosComplement(5);
        var sum = BinaryIntegerMath.AddTwosComplement(a, b);

        Assert.Equal(8, BinaryIntegerMath.FromTwosComplement(sum));

        var result = BinaryIntegerMath.AddInTwosComplement(7, -3);
        Assert.Equal(4, result.DecimalValue);
    }

    [Fact]
    public void NegateAndSubtractInTwosComplement_Work()
    {
        var valueBits = BinaryIntegerMath.ToTwosComplement(12);
        var negated = BinaryIntegerMath.NegateTwosComplement(valueBits);

        Assert.Equal(-12, BinaryIntegerMath.FromTwosComplement(negated));

        IntegerOperationResult result = BinaryIntegerMath.SubtractInTwosComplement(-2, 5);
        Assert.Equal(-7, result.DecimalValue);
    }

    [Fact]
    public void MultiplyInSignMagnitude_Works()
    {
        var result = BinaryIntegerMath.MultiplyInSignMagnitude(-6, 7);

        Assert.Equal(-42, result.DecimalValue);
        Assert.Equal(-42, BinaryIntegerMath.FromSignMagnitude(result.Bits));
    }

    [Fact]
    public void MultiplyInSignMagnitude_Overflow_Throws()
    {
        Assert.Throws<OverflowException>(() => BinaryIntegerMath.MultiplyInSignMagnitude(50_000, 50_000));
    }

    [Fact]
    public void DivideInSignMagnitude_WorksWithPrecision()
    {
        var result = BinaryIntegerMath.DivideInSignMagnitude(7, 2);

        Assert.Equal(3.5m, result.DecimalValue);
        Assert.Equal(350000, BinaryIntegerMath.FromSignMagnitude(result.Bits));
        Assert.Equal(5, result.PrecisionDigits);
    }

    [Fact]
    public void DivideInSignMagnitude_NegativeAndValidation_Work()
    {
        var result = BinaryIntegerMath.DivideInSignMagnitude(-1, 8);

        Assert.Equal(-0.125m, result.DecimalValue);

        Assert.Throws<DivideByZeroException>(() => BinaryIntegerMath.DivideInSignMagnitude(10, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => BinaryIntegerMath.DivideInSignMagnitude(10, 2, 10));
    }

    [Fact]
    public void BitHelpers_ValidationAndConversion_Work()
    {
        var valid = new int[32];
        valid[31] = 1;

        BitHelpers.Validate32Bits(valid, nameof(valid));

        Assert.Throws<ArgumentException>(() => BitHelpers.Validate32Bits([0, 1], "bits"));

        var invalidBit = new int[32];
        invalidBit[0] = 2;
        Assert.Throws<ArgumentException>(() => BitHelpers.Validate32Bits(invalidBit, "bits"));

        var nibble = BitHelpers.ToBits(13, 4);
        Assert.Equal("1101", BitHelpers.ToBitString(nibble, false));
        Assert.Equal(13, BitHelpers.BitsToInt(nibble));

        Assert.Throws<ArgumentOutOfRangeException>(() => BitHelpers.ToBits(-1, 4));
        Assert.Throws<OverflowException>(() => BitHelpers.ToBits(16, 4));
        Assert.Throws<ArgumentException>(() => BitHelpers.BitsToInt([1, 0, 2]));
    }

    [Fact]
    public void Ieee754_Zero_RoundTrip_Works()
    {
        var bits = Ieee754Math.ToIeee754(0);
        var value = Ieee754Math.FromIeee754(bits);

        Assert.Equal("00000000000000000000000000000000", BitHelpers.ToBitString(bits, false));
        Assert.Equal(0d, value);
    }

    [Fact]
    public void Ieee754_KnownValue_RoundTrip_Works()
    {
        var bits = Ieee754Math.ToIeee754(5.25);
        var value = Ieee754Math.FromIeee754(bits);

        Assert.Equal("01000000101010000000000000000000", BitHelpers.ToBitString(bits, false));
        Assert.InRange(value, 5.249999, 5.250001);
    }

    [Fact]
    public void Ieee754_Subnormal_RoundTrip_Works()
    {
        var bits = Ieee754Math.ToIeee754(1e-40);
        var value = Ieee754Math.FromIeee754(bits);

        Assert.True(value > 0);
        Assert.True(value < 1e-37);
    }

    [Fact]
    public void Ieee754_Operations_Work()
    {
        var add = Ieee754Math.Operate(1.5, 2.25, FloatingOperation.Add);
        var sub = Ieee754Math.Operate(5.5, 2.0, FloatingOperation.Subtract);
        var mul = Ieee754Math.Operate(1.5, 4.0, FloatingOperation.Multiply);
        var div = Ieee754Math.Operate(7.0, 2.0, FloatingOperation.Divide);

        Assert.InRange(add.DecimalValue, 3.749999, 3.750001);
        Assert.InRange(sub.DecimalValue, 3.499999, 3.500001);
        Assert.InRange(mul.DecimalValue, 5.999999, 6.000001);
        Assert.InRange(div.DecimalValue, 3.499999, 3.500001);
    }

    [Fact]
    public void Ieee754_ValidationErrors_Work()
    {
        Assert.Throws<ArgumentException>(() => Ieee754Math.ToIeee754(double.NaN));
        Assert.Throws<ArgumentException>(() => Ieee754Math.ToIeee754(double.PositiveInfinity));

        var invalid = new int[32];
        for (var i = 1; i <= 8; i++)
        {
            invalid[i] = 1;
        }

        Assert.Throws<ArgumentException>(() => Ieee754Math.FromIeee754(invalid));
        Assert.Throws<DivideByZeroException>(() => Ieee754Math.Operate(1, 0, FloatingOperation.Divide));
        Assert.Throws<ArgumentOutOfRangeException>(() => Ieee754Math.Operate(1, 1, (FloatingOperation)999));
    }

    [Fact]
    public void GrayBcd_Encode_Works()
    {
        var encoded = BcdMath.EncodeTo32Bits(59);

        Assert.EndsWith("01111101", BitHelpers.ToBitString(encoded, false));
    }

    [Fact]
    public void GrayBcd_Add_UsesDigitCarryAcrossNibbles()
    {
        var result = BcdMath.Add(59, 41);

        Assert.Equal(100, result.DecimalValue);
        Assert.EndsWith("000100000000", BitHelpers.ToBitString(result.Bits, false));
        Assert.Equal(32, result.Bits.Length);
    }

    [Fact]
    public void GrayBcd_Add_SingleDigitCarry_Works()
    {
        var result = BcdMath.Add(8, 7);

        Assert.Equal(15, result.DecimalValue);
        Assert.EndsWith("00010111", BitHelpers.ToBitString(result.Bits, false));
    }

    [Fact]
    public void GrayBcd_ValidationErrors_Work()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BcdMath.EncodeTo32Bits(-1));
        Assert.Throws<OverflowException>(() => BcdMath.EncodeTo32Bits(100_000_000));

        Assert.Throws<ArgumentOutOfRangeException>(() => BcdMath.Add(-1, 10));
        Assert.Throws<OverflowException>(() => BcdMath.Add(99_999_999, 1));
    }
}
