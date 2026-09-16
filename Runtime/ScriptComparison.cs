namespace RPGFramework.Field
{
    /// <summary>
    /// How an <c>IF</c> compares its two values. The numbers are bytecode, so they must not change.
    /// <br /><br />
    /// The comparison is written into the script as a byte; naming the values means an author picks
    /// "is equal to" from a list rather than remembering that 0 means equality.
    /// </summary>
    public enum ScriptComparison : byte
    {
        Equal              = 0x0,
        NotEqual           = 0x1,
        GreaterThan        = 0x2,
        LessThan           = 0x3,
        GreaterThanOrEqual = 0x4,
        LessThanOrEqual    = 0x5,

        /// <summary>Any bit set in both values.</summary>
        AnyBitInCommon = 0x6,

        /// <summary>Any bit set in one value but not the other.</summary>
        AnyBitDifferent = 0x7,

        /// <summary>Any bit set in either value.</summary>
        AnyBitSet = 0x8,

        /// <summary>The bit at the position the second value names is set.</summary>
        BitIsSet = 0x9,

        /// <summary>The bit at the position the second value names is clear.</summary>
        BitIsClear = 0xA
    }

    public static class ScriptComparisonExtensions
    {
        /// <summary>
        /// The comparisons that test bits, which mean nothing for a float.
        /// </summary>
        public static bool IsBitTest(this ScriptComparison comparison)
        {
            bool isBitTest = comparison == ScriptComparison.AnyBitInCommon  ||
                             comparison == ScriptComparison.AnyBitDifferent ||
                             comparison == ScriptComparison.AnyBitSet       ||
                             comparison == ScriptComparison.BitIsSet        ||
                             comparison == ScriptComparison.BitIsClear;

            return isBitTest;
        }

        /// <summary>
        /// How the comparison is written in a script, so <c>$flag == 1</c> reads as it would anywhere
        /// else. The bit tests have no operator, so they use their names.
        /// </summary>
        public static string ToScriptText(this ScriptComparison comparison)
        {
            switch (comparison)
            {
                case ScriptComparison.Equal:              return "==";
                case ScriptComparison.NotEqual:           return "!=";
                case ScriptComparison.GreaterThan:        return ">";
                case ScriptComparison.LessThan:           return "<";
                case ScriptComparison.GreaterThanOrEqual: return ">=";
                case ScriptComparison.LessThanOrEqual:    return "<=";
                case ScriptComparison.AnyBitInCommon:     return "&";
                case ScriptComparison.AnyBitDifferent:    return "^";
                case ScriptComparison.AnyBitSet:          return "|";
                case ScriptComparison.BitIsSet:           return "bit_set";
                case ScriptComparison.BitIsClear:         return "bit_clear";
                default:                                  return "==";
            }
        }

        public static bool TryParse(string text, out ScriptComparison comparison)
        {
            switch (text)
            {
                case "==":
                    comparison = ScriptComparison.Equal;
                    return true;
                case "!=":
                    comparison = ScriptComparison.NotEqual;
                    return true;
                case ">":
                    comparison = ScriptComparison.GreaterThan;
                    return true;
                case "<":
                    comparison = ScriptComparison.LessThan;
                    return true;
                case ">=":
                    comparison = ScriptComparison.GreaterThanOrEqual;
                    return true;
                case "<=":
                    comparison = ScriptComparison.LessThanOrEqual;
                    return true;
                case "&":
                    comparison = ScriptComparison.AnyBitInCommon;
                    return true;
                case "^":
                    comparison = ScriptComparison.AnyBitDifferent;
                    return true;
                case "|":
                    comparison = ScriptComparison.AnyBitSet;
                    return true;
                case "bit_set":
                    comparison = ScriptComparison.BitIsSet;
                    return true;
                case "bit_clear":
                    comparison = ScriptComparison.BitIsClear;
                    return true;
            }

            comparison = ScriptComparison.Equal;

            bool parsed = false;

            return parsed;
        }
    }
}