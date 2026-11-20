using NUnit.Framework;
using RPGFramework.Core.Memory;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.GameOpCodes
{
    /// <summary>
    /// Exported scripts hold the opcodes' values and their arguments as they were written, so export records the table it
    /// compiled with and a player build is refused once it has changed, the framework's or the game's.
    /// </summary>
    internal sealed class WhenTheOpCodeTableChangesUnderExportedFields
    {
        [Test]
        public void ThenTheSameTableHashesTheSame()
        {
            Assert.AreEqual(Hash(typeof(FieldScriptOpCode), typeof(TestGameOpCode)), Hash(typeof(FieldScriptOpCode), typeof(TestGameOpCode)));
        }

        [Test]
        public void ThenAGamesOpCodesChangeIt()
        {
            Assert.AreNotEqual(Hash(typeof(FieldScriptOpCode)), Hash(typeof(FieldScriptOpCode), typeof(TestGameOpCode)));
        }

        [Test]
        public void ThenAnArgumentRetypedChangesIt()
        {
            Assert.AreNotEqual(Hash(typeof(HashedOpCode)), Hash(typeof(RetypedOpCode)));
        }

        [Test]
        public void ThenAVariablesWidthChangesIt()
        {
            Assert.AreNotEqual(Hash(typeof(HashedOpCode)), Hash(typeof(ResizedOpCode)));
        }

        [Test]
        public void ThenArgumentsReorderedChangeIt()
        {
            Assert.AreNotEqual(Hash(typeof(HashedOpCode)), Hash(typeof(ReorderedOpCode)));
        }

        [Test]
        public void ThenAnArgumentAddedChangesIt()
        {
            Assert.AreNotEqual(Hash(typeof(HashedOpCode)), Hash(typeof(ExtendedOpCode)));
        }

        [Test]
        public void ThenAnotherLayoutChangesIt()
        {
            Assert.AreNotEqual(Hash(typeof(HashedOpCode)), Hash(typeof(RelaidOpCode)));
        }

        [Test]
        public void ThenANameOrADescriptionLeavesIt()
        {
            Assert.AreEqual(Hash(typeof(HashedOpCode)), Hash(typeof(RedescribedOpCode)));
        }

        private static ulong Hash(params System.Type[] tables)
        {
            return FieldExport.OpCodeHash(new FieldOpCodeCatalogue(tables));
        }
    }

    // One opcode each, the same value and script name, differing from HashedOpCode in one thing. Below a game's range,
    // so the read check, which looks a game's value up across every table the tests hold, never meets them.

    internal enum HashedOpCode : ushort
    {
        [FieldOpCode("TEST_HASHED", ArgumentLayout.Sequential)]
        [Argument(0, "count",  ArgumentType.Byte)]
        [Argument(1, "result", ArgumentType.Variable, VariableWidth.Int)]
        Hashed = 0x7E00
    }

    internal enum RetypedOpCode : ushort
    {
        [FieldOpCode("TEST_HASHED", ArgumentLayout.Sequential)]
        [Argument(0, "count",  ArgumentType.Int)]
        [Argument(1, "result", ArgumentType.Variable, VariableWidth.Int)]
        Hashed = 0x7E00
    }

    internal enum ResizedOpCode : ushort
    {
        [FieldOpCode("TEST_HASHED", ArgumentLayout.Sequential)]
        [Argument(0, "count",  ArgumentType.Byte)]
        [Argument(1, "result", ArgumentType.Variable, VariableWidth.Long)]
        Hashed = 0x7E00
    }

    internal enum ReorderedOpCode : ushort
    {
        [FieldOpCode("TEST_HASHED", ArgumentLayout.Sequential)]
        [Argument(1, "count",  ArgumentType.Byte)]
        [Argument(0, "result", ArgumentType.Variable, VariableWidth.Int)]
        Hashed = 0x7E00
    }

    internal enum ExtendedOpCode : ushort
    {
        [FieldOpCode("TEST_HASHED", ArgumentLayout.Sequential)]
        [Argument(0, "count",  ArgumentType.Byte)]
        [Argument(1, "result", ArgumentType.Variable, VariableWidth.Int)]
        [Argument(2, "again",  ArgumentType.Bool)]
        Hashed = 0x7E00
    }

    internal enum RelaidOpCode : ushort
    {
        [FieldOpCode("TEST_HASHED", ArgumentLayout.BankBinary)]
        [Argument(0, "count",  ArgumentType.Byte)]
        [Argument(1, "result", ArgumentType.Variable, VariableWidth.Int)]
        Hashed = 0x7E00
    }

    internal enum RedescribedOpCode : ushort
    {
        [FieldOpCode("TEST_HASHED", ArgumentLayout.Sequential, Summary = "Count into a variable")]
        [Argument(0, "amount", ArgumentType.Byte, Description = "how many", Default = "1")]
        [Argument(1, "total",  ArgumentType.Variable, VariableWidth.Int)]
        Hashed = 0x7E00
    }
}
