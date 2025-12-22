// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using System.Numerics;
using System.Runtime.CompilerServices;
using static BigFloatLibrary.BigFloatNumerics;

namespace BigFloatLibrary;

/// <summary>
/// Optional extended division logic for large operands.
/// </summary>
public readonly partial struct BigFloat
{
    static partial void OnDivideLargeNumbers(BigFloat divisor, BigFloat dividend, ref bool handled, ref BigFloat result)
    {
        int outputSize = Math.Min(divisor.Size, dividend.Size);

        if (divisor._mantissa >> (divisor.Size - dividend.Size) <= dividend._mantissa)
        {
            outputSize--;
        }

        int wantedSizeForT = dividend.Size + outputSize + GuardBits;
        int leftShiftTBy = wantedSizeForT - divisor.Size;

        BigInteger numeratorAbs = BigInteger.Abs(divisor._mantissa) << leftShiftTBy;
        BigInteger denominatorAbs = BigInteger.Abs(dividend._mantissa);

        if (denominatorAbs.IsZero)
        {
            throw new DivideByZeroException("Division by zero");
        }

        BigInteger quotientAbs = DivideBurnikelZiegler(numeratorAbs, denominatorAbs);
        BigInteger resIntPart = divisor._mantissa.Sign == dividend._mantissa.Sign
            ? quotientAbs
            : BigInteger.Negate(quotientAbs);

        int resScalePart = divisor.Scale - dividend.Scale - leftShiftTBy + GuardBits;
        int sizePart = MantissaSize(resIntPart);

        result = new BigFloat(resIntPart, resScalePart, sizePart);
        handled = true;
    }

    private static BigInteger DivideBurnikelZiegler(BigInteger numerator, BigInteger denominator)
    {
        if (numerator.IsZero || numerator < denominator)
        {
            return BigInteger.Zero;
        }

        var (quotient, _) = DivideBurnikelZieglerCore(numerator, denominator);
        return quotient;
    }

    private static (BigInteger Quotient, BigInteger Remainder) DivideBurnikelZieglerCore(BigInteger numerator, BigInteger denominator)
    {
        const int WordBits = 32;
        const int BaseCaseWordThreshold = 16;

        int numeratorWords = GetWordLength(numerator, WordBits);
        int denominatorWords = GetWordLength(denominator, WordBits);

        if (numeratorWords <= BaseCaseWordThreshold || denominatorWords <= BaseCaseWordThreshold)
        {
            BigInteger quotient = BigInteger.DivRem(numerator, denominator, out BigInteger remainder);
            return (quotient, remainder);
        }

        int blockWords = (denominatorWords + 1) / 2;
        int blockShift = blockWords * WordBits;
        BigInteger blockMask = (BigInteger.One << blockShift) - 1;

        int totalBlocks = (numeratorWords + blockWords - 1) / blockWords;
        if ((totalBlocks & 1) == 1)
        {
            totalBlocks++;
        }

        BigInteger quotientAggregate = BigInteger.Zero;
        BigInteger remainderAggregate = BigInteger.Zero;

        for (int blockIndex = totalBlocks - 1; blockIndex >= 0; blockIndex -= 2)
        {
            BigInteger highBlock = ExtractBlock(numerator, blockIndex, blockShift, blockMask);
            BigInteger lowBlock = blockIndex > 0
                ? ExtractBlock(numerator, blockIndex - 1, blockShift, blockMask)
                : BigInteger.Zero;

            BigInteger uHat = (remainderAggregate << blockShift) + highBlock;
            uHat = (uHat << blockShift) + lowBlock;

            var (qHat, newRemainder) = Divide2n1n(uHat, denominator, blockWords, WordBits);

            quotientAggregate = (quotientAggregate << (2 * blockShift)) + qHat;
            remainderAggregate = newRemainder;
        }

        return (quotientAggregate, remainderAggregate);
    }

    private static (BigInteger Quotient, BigInteger Remainder) Divide2n1n(BigInteger numerator, BigInteger denominator, int blockWords, int wordBits)
    {
        const int BaseCaseWordThreshold = 16;

        int numeratorWords = GetWordLength(numerator, wordBits);
        int denominatorWords = GetWordLength(denominator, wordBits);

        if (numeratorWords <= BaseCaseWordThreshold || denominatorWords <= BaseCaseWordThreshold)
        {
            BigInteger quotient = BigInteger.DivRem(numerator, denominator, out BigInteger remainder);
            return (quotient, remainder);
        }

        int blockShift = blockWords * wordBits;
        BigInteger blockMask = (BigInteger.One << blockShift) - 1;

        BigInteger v1 = denominator >> blockShift;
        BigInteger u1 = numerator >> blockShift;
        BigInteger u0 = numerator & blockMask;

        if (v1.IsZero)
        {
            BigInteger quotient = BigInteger.DivRem(numerator, denominator, out BigInteger remainder);
            return (quotient, remainder);
        }

        var (q1, r1) = DivideBurnikelZieglerCore(u1, v1);
        BigInteger uPrime = (r1 << blockShift) + u0;
        var (q0, _) = DivideBurnikelZieglerCore(uPrime, v1);

        BigInteger quotientApprox = (q1 << blockShift) + q0;
        BigInteger correctedRemainder = numerator - quotientApprox * denominator;

        if (correctedRemainder.Sign < 0)
        {
            BigInteger adjustment = BigInteger.DivRem(BigInteger.Negate(correctedRemainder), denominator, out BigInteger remainder);
            if (!remainder.IsZero)
            {
                adjustment += BigInteger.One;
            }

            quotientApprox -= adjustment;
            correctedRemainder += adjustment * denominator;
        }
        else
        {
            if (correctedRemainder >= denominator)
            {
                BigInteger adjustment = correctedRemainder / denominator;
                quotientApprox += adjustment;
                correctedRemainder -= adjustment * denominator;
            }
        }

        return (quotientApprox, correctedRemainder);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetWordLength(BigInteger value, int wordBits)
    {
        int bitLength = (int)BigInteger.Abs(value).GetBitLength();
        if (bitLength == 0)
        {
            return 0;
        }

        return (bitLength + wordBits - 1) / wordBits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BigInteger ExtractBlock(BigInteger value, int blockIndex, int blockShift, BigInteger blockMask)
    {
        int shiftBits = blockIndex * blockShift;
        return (value >> shiftBits) & blockMask;
    }
}
