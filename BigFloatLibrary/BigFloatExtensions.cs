using BigFloatLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigFloatLibrary;

// This should be a separate, non-generic static class
public static class BigFloatExtensions
{
    /// <summary>
    /// Left shift - Increases the size by adding least-significant zero bits. 
    /// i.e. The precision is artificially enhanced. 
    /// </summary>
    /// <param name="x">The value the shift should be applied to.</param>
    /// <param name="shift">The number of bits to shift left.</param>
    /// <returns>A new BigFloat with the internal 'int' up shifted.</returns>
    public static BigFloat LeftShiftMantissa(this BigFloat value, int bits)
            => BigFloat.CreateFromRawComponents(value.Mantissa << bits, value.Scale, value._size + bits);

    /// <summary>
    /// Right shift - Decreases the size by removing the least-significant bits. 
    /// i.e. The precision is reduced. 
    /// No rounding is performed and Scale is unchanged. 
    /// </summary>
    /// <param name="value">The value the shift should be applied to.</param>
    /// <param name="bits">The number of bits to shift right.</param>
    /// <returns>A new BigFloat with the internal 'int' down shifted.</returns>
    public static BigFloat RightShiftMantissa(this BigFloat value, int bits)
        => BigFloat.CreateFromRawComponents(value.Mantissa >> bits, value.Scale, value._size - bits);

}