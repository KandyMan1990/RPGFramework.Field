using System;
using RPGFramework.Core.Memory;

namespace RPGFramework.Field
{
    /// <summary>
    /// Integer maths for every integer width. A value is held as its bits in a <c>ulong</c> — the width's own bits
    /// and nothing above them — and is read as signed only where the answer depends on it.
    /// </summary>
    internal static class ScriptArithmetic
    {
        internal static int BitCount(VariableWidth width)
        {
            int bitCount = width.GetByteCount() * 8;

            return bitCount;
        }

        internal static ulong Truncate(ulong bits, VariableWidth width)
        {
            int   bitCount  = BitCount(width);
            ulong truncated = bitCount == 64 ? bits : bits & ((1UL << bitCount) - 1);

            return truncated;
        }

        internal static long ToSigned(ulong bits, VariableWidth width)
        {
            int  shift  = 64 - BitCount(width);
            long signed = (long)(bits << shift) >> shift;

            return signed;
        }

        /// <summary>
        /// A value of one width as another, the way a cast converts it: extended by the source's sign, then cut
        /// to the target's width.
        /// </summary>
        internal static ulong Convert(ulong bits, VariableWidth from, VariableWidth to)
        {
            ulong extended  = from.IsSigned() ? (ulong)ToSigned(bits, from) : bits;
            ulong converted = Truncate(extended, to);

            return converted;
        }

        internal static float ToFloat(ulong bits, VariableWidth width)
        {
            float value = width.IsSigned() ? ToSigned(bits, width) : bits;

            return value;
        }

        internal static ulong Add(ulong a, ulong b, VariableWidth width)
        {
            ulong sum = Truncate(unchecked(a + b), width);

            return sum;
        }

        internal static ulong Subtract(ulong a, ulong b, VariableWidth width)
        {
            ulong difference = Truncate(unchecked(a - b), width);

            return difference;
        }

        internal static ulong Multiply(ulong a, ulong b, VariableWidth width)
        {
            ulong product = Truncate(unchecked(a * b), width);

            return product;
        }

        internal static ulong BitwiseAnd(ulong a, ulong b, VariableWidth width)
        {
            ulong result = a & b;

            return result;
        }

        internal static ulong BitwiseOr(ulong a, ulong b, VariableWidth width)
        {
            ulong result = a | b;

            return result;
        }

        internal static ulong BitwiseXor(ulong a, ulong b, VariableWidth width)
        {
            ulong result = a ^ b;

            return result;
        }

        /// <returns>False when dividing by zero.</returns>
        internal static bool TryDivide(ulong a, ulong b, VariableWidth width, out ulong quotient)
        {
            if (b == 0)
            {
                quotient = a;
                return false;
            }

            if (!width.IsSigned())
            {
                quotient = a / b;
                return true;
            }

            long dividend = ToSigned(a, width);
            long divisor  = ToSigned(b, width);

            // The smallest long divided by -1 overflows and throws; it wraps here as it does at every narrower width.
            long signedQuotient = divisor == -1 ? unchecked(-dividend) : dividend / divisor;

            quotient = Truncate((ulong)signedQuotient, width);
            return true;
        }

        /// <returns>False when dividing by zero.</returns>
        internal static bool TryRemainder(ulong a, ulong b, VariableWidth width, out ulong remainder)
        {
            if (b == 0)
            {
                remainder = a;
                return false;
            }

            if (!width.IsSigned())
            {
                remainder = a % b;
                return true;
            }

            long dividend = ToSigned(a, width);
            long divisor  = ToSigned(b, width);

            // As in TryDivide: the smallest long % -1 throws, and the remainder is always 0.
            long signedRemainder = divisor == -1 ? 0 : dividend % divisor;

            remainder = Truncate((ulong)signedRemainder, width);
            return true;
        }

        internal static ulong AddClamped(ulong a, ulong b, VariableWidth width)
        {
            if (!width.IsSigned())
            {
                ulong max     = Truncate(ulong.MaxValue, width);
                ulong sum     = unchecked(a + b);
                ulong clamped = sum < a || sum > max ? max : sum;

                return clamped;
            }

            long x = ToSigned(a, width);
            long y = ToSigned(b, width);

            long signedSum = unchecked(x + y);

            if (((x ^ signedSum) & (y ^ signedSum)) < 0)
            {
                signedSum = x < 0 ? long.MinValue : long.MaxValue;
            }

            ulong result = ClampSigned(signedSum, width);

            return result;
        }

        internal static ulong SubtractClamped(ulong a, ulong b, VariableWidth width)
        {
            if (!width.IsSigned())
            {
                ulong clamped = a < b ? 0 : a - b;

                return clamped;
            }

            long x = ToSigned(a, width);
            long y = ToSigned(b, width);

            long signedDifference = unchecked(x - y);

            if (((x ^ y) & (x ^ signedDifference)) < 0)
            {
                signedDifference = x < 0 ? long.MinValue : long.MaxValue;
            }

            ulong result = ClampSigned(signedDifference, width);

            return result;
        }

        /// <returns>False when the index is past the width's last bit.</returns>
        internal static bool TrySetBit(ulong bits, ulong index, VariableWidth width, bool set, out ulong result)
        {
            if (index >= (ulong)BitCount(width))
            {
                result = bits;
                return false;
            }

            ulong mask = 1UL << (int)index;

            result = set ? bits | mask : bits & ~mask;
            return true;
        }

        internal static bool Compare(ulong a, ulong b, VariableWidth width, ScriptComparison comparison)
        {
            bool signed = width.IsSigned();
            long x      = signed ? ToSigned(a, width) : 0;
            long y      = signed ? ToSigned(b, width) : 0;

            bool result = comparison switch
                          {
                              ScriptComparison.Equal              => a == b,
                              ScriptComparison.NotEqual           => a != b,
                              ScriptComparison.GreaterThan        => signed ? x > y : a > b,
                              ScriptComparison.LessThan           => signed ? x < y : a < b,
                              ScriptComparison.GreaterThanOrEqual => signed ? x >= y : a >= b,
                              ScriptComparison.LessThanOrEqual    => signed ? x <= y : a <= b,
                              ScriptComparison.AnyBitInCommon     => (a & b) != 0,
                              ScriptComparison.AnyBitDifferent    => (a ^ b) != 0,
                              ScriptComparison.AnyBitSet          => (a | b) != 0,
                              ScriptComparison.BitIsSet           => b < (ulong)BitCount(width) && ((a >> (int)b) & 1) != 0,
                              ScriptComparison.BitIsClear         => b >= (ulong)BitCount(width) || ((a >> (int)b) & 1) == 0,
                              _                                   => throw new InvalidOperationException($"{nameof(ScriptArithmetic)}::{nameof(Compare)} Unknown comparison [{comparison}]")
                          };

            return result;
        }

        private static ulong ClampSigned(long value, VariableWidth width)
        {
            long max     = (long)(Truncate(ulong.MaxValue, width) >> 1);
            long min     = -max - 1;
            long clamped = Math.Min(Math.Max(value, min), max);

            ulong result = Truncate((ulong)clamped, width);

            return result;
        }
    }
}
