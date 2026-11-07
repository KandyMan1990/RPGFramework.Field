using RPGFramework.Core.Memory;

namespace RPGFramework.Field.Tests.GameOpCodes
{
    /// <summary>
    /// A game's own table, as a game declares one. The project's catalogue leaves it out, as the tests are editor-only,
    /// and its values sit at the top of the range, where a game's own would not meet them.
    /// </summary>
    internal enum TestGameOpCode : ushort
    {
        [FieldOpCode("TEST_TAKE_TWO", ArgumentLayout.Sequential, Summary = "Take a byte and an int")]
        [Argument(0, "count",  ArgumentType.Byte)]
        [Argument(1, "amount", ArgumentType.Int)]
        TakeTwo = 0xFF00,

        [FieldOpCode("TEST_WRITE", ArgumentLayout.Sequential, Summary = "Write a value to a variable")]
        [Argument(0, "result", ArgumentType.Variable, VariableWidth.Int)]
        [Argument(1, "value",  ArgumentType.Int)]
        Write = 0xFF01,

        [FieldOpCode("TEST_NAME", ArgumentLayout.Sequential, Summary = "Take a track's name")]
        [Argument(0, "track", ArgumentType.MusicName)]
        Name = 0xFF02
    }

    /// <summary>A game's table with everything export refuses in one.</summary>
    internal enum InvalidGameOpCode : ushort
    {
        [FieldOpCode("TEST_LOW", ArgumentLayout.Sequential)]
        Low = 0x7F00,

        [FieldOpCode("TEST_SHARED_A", ArgumentLayout.Sequential)]
        SharedA = 0xFE01,

        [FieldOpCode("TEST_SHARED_B", ArgumentLayout.Sequential)]
        SharedB = 0xFE01,

        [FieldOpCode("RETURN", ArgumentLayout.Sequential)]
        Named = 0xFE02,

        [FieldOpCode("LABEL", ArgumentLayout.Sequential)]
        Word = 0xFE03,

        [FieldOpCode("TEST_BANK", ArgumentLayout.BankUnary)]
        [Argument(0, "result", ArgumentType.Variable, VariableWidth.Int)]
        Bank = 0xFE04,

        [FieldOpCode("TEST_BLOCK", ArgumentLayout.Sequential, OpensBlock = true)]
        Block = 0xFE05,

        [FieldOpCode("TEST_JUMP", ArgumentLayout.Sequential)]
        [Argument(0, "target", ArgumentType.Label)]
        Jump = 0xFE06,

        [FieldOpCode("TEST_LONG", ArgumentLayout.Sequential)]
        [Argument(0, "result", ArgumentType.Variable, VariableWidth.Long)]
        Long = 0xFE07
    }

    /// <summary>A game's table not of <c>ushort</c>s, as no opcode can be wider.</summary>
    internal enum WideGameOpCode
    {
        [FieldOpCode("TEST_WIDE", ArgumentLayout.Sequential)]
        Wide = 0xFE10
    }
}
