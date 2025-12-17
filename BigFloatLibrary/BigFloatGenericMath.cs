using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
#nullable enable

namespace BigFloatLibrary;

public readonly partial struct BigFloat : INumberBase<BigFloat>
{
    static BigFloat INumberBase<BigFloat>.One => OneWithAccuracy(0);

    static int INumberBase<BigFloat>.Radix => 2;

    static BigFloat INumberBase<BigFloat>.Zero => ZeroWithAccuracy(0);

    static BigFloat IAdditiveIdentity<BigFloat, BigFloat>.AdditiveIdentity => ZeroWithAccuracy(0);

    static BigFloat IMultiplicativeIdentity<BigFloat, BigFloat>.MultiplicativeIdentity => OneWithAccuracy(0);

    static BigFloat INumberBase<BigFloat>.Abs(BigFloat value) => Abs(value);

    static bool INumberBase<BigFloat>.IsCanonical(BigFloat value)
    {
        if (value._size == 0)
        {
            return true;
        }

        BigInteger mask = (BigInteger.One << GuardBits) - 1;
        return (value._mantissa & mask) == BigInteger.Zero;
    }

    static bool INumberBase<BigFloat>.IsComplexNumber(BigFloat value) => false;

    static bool INumberBase<BigFloat>.IsEvenInteger(BigFloat value)
        => value.IsInteger && (BigInteger.Abs(value.GetIntegralValue()) & BigInteger.One) == BigInteger.Zero;

    static bool INumberBase<BigFloat>.IsFinite(BigFloat value) => true;

    static bool INumberBase<BigFloat>.IsImaginaryNumber(BigFloat value) => false;

    static bool INumberBase<BigFloat>.IsInfinity(BigFloat value) => false;

    static bool INumberBase<BigFloat>.IsInteger(BigFloat value) => value.IsInteger;

    static bool INumberBase<BigFloat>.IsNaN(BigFloat value) => false;

    static bool INumberBase<BigFloat>.IsNegative(BigFloat value) => value.IsNegative;

    static bool INumberBase<BigFloat>.IsNegativeInfinity(BigFloat value) => false;

    static bool INumberBase<BigFloat>.IsNormal(BigFloat value) => !value.IsZero;

    static bool INumberBase<BigFloat>.IsOddInteger(BigFloat value)
        => value.IsInteger && (BigInteger.Abs(value.GetIntegralValue()) & BigInteger.One) != BigInteger.Zero;

    static bool INumberBase<BigFloat>.IsPositive(BigFloat value) => value.IsPositive || value.IsZero;

    static bool INumberBase<BigFloat>.IsPositiveInfinity(BigFloat value) => false;

    static bool INumberBase<BigFloat>.IsRealNumber(BigFloat value) => true;

    static bool INumberBase<BigFloat>.IsSubnormal(BigFloat value) => false;

    static bool INumberBase<BigFloat>.IsZero(BigFloat value) => value.IsZero;

    static BigFloat INumberBase<BigFloat>.MaxMagnitude(BigFloat x, BigFloat y)
        => MaxMagnitudeCore(x, y);

    static BigFloat INumberBase<BigFloat>.MaxMagnitudeNumber(BigFloat x, BigFloat y)
        => MaxMagnitudeCore(x, y);

    static BigFloat INumberBase<BigFloat>.MinMagnitude(BigFloat x, BigFloat y)
        => MinMagnitudeCore(x, y);

    static BigFloat INumberBase<BigFloat>.MinMagnitudeNumber(BigFloat x, BigFloat y)
        => MinMagnitudeCore(x, y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BigFloat MaxMagnitudeCore(BigFloat x, BigFloat y)
    {
        BigFloat ax = Abs(x);
        BigFloat ay = Abs(y);
        int cmp = ax.CompareTo(ay);
        return cmp > 0 ? x : cmp < 0 ? y : BigFloat.Max(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BigFloat MinMagnitudeCore(BigFloat x, BigFloat y)
    {
        BigFloat ax = Abs(x);
        BigFloat ay = Abs(y);
        int cmp = ax.CompareTo(ay);
        return cmp < 0 ? x : cmp > 0 ? y : BigFloat.Min(x, y);
    }

    static BigFloat INumberBase<BigFloat>.Parse(string s, NumberStyles style, IFormatProvider? provider)
    {
        ValidateNumberStyle(style);
        return Parse(s);
    }

    static BigFloat INumberBase<BigFloat>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider)
    {
        ValidateNumberStyle(style);
        return Parse(new string(s));
    }

    static BigFloat IParsable<BigFloat>.Parse(string s, IFormatProvider? provider) => Parse(s);

    static bool IParsable<BigFloat>.TryParse(string? s, IFormatProvider? provider, out BigFloat result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }

        return TryParse(s, out result);
    }

    static BigFloat ISpanParsable<BigFloat>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(new string(s));

    static bool ISpanParsable<BigFloat>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out BigFloat result)
        => TryParse(new string(s), out result);

    public static bool TryConvertFromChecked<TOther>(TOther value, [MaybeNullWhen(false)] out BigFloat result)
        where TOther : INumberBase<TOther>
        => TryConvertFromSupported(value, allowNonFinite: false, out result);

    public static bool TryConvertFromSaturating<TOther>(TOther value, [MaybeNullWhen(false)] out BigFloat result)
        where TOther : INumberBase<TOther>
        => TryConvertFromSupported(value, allowNonFinite: false, out result);

    public static bool TryConvertFromTruncating<TOther>(TOther value, [MaybeNullWhen(false)] out BigFloat result)
        where TOther : INumberBase<TOther>
        => TryConvertFromSupported(value, allowNonFinite: false, out result);

    public static bool TryConvertToChecked<TOther>(BigFloat value, [MaybeNullWhen(false)] out TOther result)
        where TOther : INumberBase<TOther>
    {
        object? boxed = null;

        if (typeof(TOther) == typeof(BigFloat))
        {
            boxed = value;
        }
        else if (typeof(TOther) == typeof(double))
        {
            EnsureFiniteDouble(value);
            boxed = (double)value;
        }
        else if (typeof(TOther) == typeof(float))
        {
            EnsureFiniteFloat(value);
            boxed = (float)value;
        }
        else if (typeof(TOther) == typeof(decimal))
        {
            boxed = (decimal)value;
        }
        else if (typeof(TOther) == typeof(byte))
        {
            boxed = checked((byte)value);
        }
        else if (typeof(TOther) == typeof(sbyte))
        {
            boxed = checked((sbyte)value);
        }
        else if (typeof(TOther) == typeof(short))
        {
            boxed = checked((short)value);
        }
        else if (typeof(TOther) == typeof(ushort))
        {
            boxed = checked((ushort)value);
        }
        else if (typeof(TOther) == typeof(int))
        {
            boxed = checked((int)value);
        }
        else if (typeof(TOther) == typeof(uint))
        {
            boxed = checked((uint)value);
        }
        else if (typeof(TOther) == typeof(long))
        {
            boxed = checked((long)value);
        }
        else if (typeof(TOther) == typeof(ulong))
        {
            boxed = checked((ulong)value);
        }
        else if (typeof(TOther) == typeof(Int128))
        {
            boxed = (Int128)value;
        }
        else if (typeof(TOther) == typeof(UInt128))
        {
            boxed = (UInt128)value;
        }
        else if (typeof(TOther) == typeof(BigInteger))
        {
            boxed = (BigInteger)value;
        }

        if (boxed is null)
        {
            result = default!;
            return false;
        }

        result = (TOther)boxed;
        return true;
    }

    public static bool TryConvertToSaturating<TOther>(BigFloat value, [MaybeNullWhen(false)] out TOther result)
        where TOther : INumberBase<TOther>
    {
        object? boxed = null;

        if (typeof(TOther) == typeof(double))
        {
            boxed = value.FitsInADoubleWithDenormalization ? (double)value : (value.Sign < 0 ? double.NegativeInfinity : double.PositiveInfinity);
        }
        else if (typeof(TOther) == typeof(float))
        {
            boxed = value.FitsInAFloatWithDenormalization ? (float)value : (value.Sign < 0 ? float.NegativeInfinity : float.PositiveInfinity);
        }
        else if (typeof(TOther) == typeof(decimal))
        {
            if (!value.FitsInADecimal)
            {
                boxed = value.Sign < 0 ? decimal.MinValue : decimal.MaxValue;
            }
            else
            {
                boxed = (decimal)value;
            }
        }
        else if (typeof(TOther) == typeof(byte))
        {
            boxed = ClampToIntegral(value, (BigInteger)byte.MinValue, (BigInteger)byte.MaxValue, v => (byte)v);
        }
        else if (typeof(TOther) == typeof(sbyte))
        {
            boxed = ClampToIntegral(value, (BigInteger)sbyte.MinValue, (BigInteger)sbyte.MaxValue, v => (sbyte)v);
        }
        else if (typeof(TOther) == typeof(short))
        {
            boxed = ClampToIntegral(value, (BigInteger)short.MinValue, (BigInteger)short.MaxValue, v => (short)v);
        }
        else if (typeof(TOther) == typeof(ushort))
        {
            boxed = ClampToIntegral(value, (BigInteger)ushort.MinValue, (BigInteger)ushort.MaxValue, v => (ushort)v);
        }
        else if (typeof(TOther) == typeof(int))
        {
            boxed = ClampToIntegral(value, (BigInteger)int.MinValue, (BigInteger)int.MaxValue, v => (int)v);
        }
        else if (typeof(TOther) == typeof(uint))
        {
            boxed = ClampToIntegral(value, (BigInteger)uint.MinValue, (BigInteger)uint.MaxValue, v => (uint)v);
        }
        else if (typeof(TOther) == typeof(long))
        {
            boxed = ClampToIntegral(value, (BigInteger)long.MinValue, (BigInteger)long.MaxValue, v => (long)v);
        }
        else if (typeof(TOther) == typeof(ulong))
        {
            boxed = ClampToIntegral(value, (BigInteger)ulong.MinValue, (BigInteger)ulong.MaxValue, v => (ulong)v);
        }
        else if (typeof(TOther) == typeof(BigFloat))
        {
            boxed = value;
        }
        else if (typeof(TOther) == typeof(BigInteger))
        {
            boxed = GetIntegralValue(value);
        }
        else if (typeof(TOther) == typeof(Int128))
        {
            boxed = ClampToIntegral(value, (BigInteger)Int128.MinValue, (BigInteger)Int128.MaxValue, v => (Int128)v);
        }
        else if (typeof(TOther) == typeof(UInt128))
        {
            boxed = ClampToIntegral(value, (BigInteger)UInt128.MinValue, (BigInteger)UInt128.MaxValue, v => (UInt128)v);
        }

        if (boxed is null)
        {
            result = default!;
            return false;
        }

        result = (TOther)boxed;
        return true;
    }

    public static bool TryConvertToTruncating<TOther>(BigFloat value, [MaybeNullWhen(false)] out TOther result)
        where TOther : INumberBase<TOther>
    {
        object? boxed = null;

        if (typeof(TOther) == typeof(BigFloat))
        {
            boxed = value;
        }
        else if (typeof(TOther) == typeof(BigInteger))
        {
            boxed = value.Truncate().GetIntegralValue();
        }
        else if (typeof(TOther) == typeof(double))
        {
            EnsureFiniteDouble(value);
            boxed = (double)value;
        }
        else if (typeof(TOther) == typeof(float))
        {
            EnsureFiniteFloat(value);
            boxed = (float)value;
        }
        else if (typeof(TOther) == typeof(decimal))
        {
            boxed = (decimal)value;
        }
        else if (typeof(TOther) == typeof(byte))
        {
            boxed = (byte)value;
        }
        else if (typeof(TOther) == typeof(sbyte))
        {
            boxed = (sbyte)value;
        }
        else if (typeof(TOther) == typeof(short))
        {
            boxed = (short)value;
        }
        else if (typeof(TOther) == typeof(ushort))
        {
            boxed = (ushort)value;
        }
        else if (typeof(TOther) == typeof(int))
        {
            boxed = (int)value;
        }
        else if (typeof(TOther) == typeof(uint))
        {
            boxed = (uint)value;
        }
        else if (typeof(TOther) == typeof(long))
        {
            boxed = (long)value;
        }
        else if (typeof(TOther) == typeof(ulong))
        {
            boxed = (ulong)value;
        }
        else if (typeof(TOther) == typeof(Int128))
        {
            boxed = (Int128)value;
        }
        else if (typeof(TOther) == typeof(UInt128))
        {
            boxed = (UInt128)value;
        }

        if (boxed is null)
        {
            result = default!;
            return false;
        }

        result = (TOther)boxed;
        return true;
    }

    static bool INumberBase<BigFloat>.TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out BigFloat result)
    {
        ValidateNumberStyle(style);
        if (s is null)
        {
            result = default;
            return false;
        }

        return TryParse(s, out result);
    }

    static bool INumberBase<BigFloat>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out BigFloat result)
    {
        ValidateNumberStyle(style);
        return TryParse(new string(s), out result);
    }

    private static void ValidateNumberStyle(NumberStyles style)
    {
        const NumberStyles Allowed = NumberStyles.Float | NumberStyles.AllowThousands;
        if ((style & ~Allowed) != 0)
        {
            throw new ArgumentException($"NumberStyles '{style}' not supported for BigFloat parsing.", nameof(style));
        }
    }

    private static void EnsureFiniteDouble(BigFloat value)
    {
        if (!value.FitsInADoubleWithDenormalization)
        {
            throw new OverflowException("BigFloat cannot be represented as a finite Double.");
        }
    }

    private static void EnsureFiniteFloat(BigFloat value)
    {
        if (!value.FitsInAFloatWithDenormalization)
        {
            throw new OverflowException("BigFloat cannot be represented as a finite Single.");
        }
    }

    private static T ClampToIntegral<T>(BigFloat value, BigInteger min, BigInteger max, Func<BigInteger, T> converter)
    {
        BigInteger truncated = value.Truncate().GetIntegralValue();

        if (truncated < min) truncated = min;
        if (truncated > max) truncated = max;

        return converter(truncated);
    }

    private static bool TryConvertFromSupported<TOther>(TOther value, bool allowNonFinite, out BigFloat result)
    {
        if (value is null)
        {
            result = default;
            return false;
        }

        switch (value)
        {
            case BigFloat bf:
                result = bf;
                return true;
            case byte b:
                result = new BigFloat(b);
                return true;
            case sbyte sb:
                result = new BigFloat(sb);
                return true;
            case short s:
                result = new BigFloat(s);
                return true;
            case ushort us:
                result = new BigFloat(us);
                return true;
            case int i:
                result = new BigFloat(i);
                return true;
            case uint ui:
                result = new BigFloat(ui);
                return true;
            case long l:
                result = new BigFloat(l);
                return true;
            case ulong ul:
                result = new BigFloat(ul);
                return true;
            case Int128 i128:
                result = new BigFloat(i128);
                return true;
            case UInt128 u128:
                result = new BigFloat(u128);
                return true;
            case float f when allowNonFinite || float.IsFinite(f):
                result = (BigFloat)f;
                return true;
            case double d when allowNonFinite || double.IsFinite(d):
                result = (BigFloat)d;
                return true;
            case decimal dec:
                result = new BigFloat(dec);
                return true;
            case BigInteger bi:
                result = (BigFloat)bi;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
