using System;
using System.Linq;
using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Compiler
{
    /// <summary>
    /// IF_BYTE is its opcode, a sources byte, two values, a comparison and a jump distance: ten bytes, then its body.
    /// END_IF writes nothing; it tells the compiler where the body stops.
    /// </summary>
    internal sealed class WhenAnIfIsCompiled
    {
        [Test]
        public void ThenItIsTheComparisonThenItsBody()
        {
            Assert.AreEqual(12, FieldScriptCompiler.Compile("IF_BYTE 1 == 1\nYIELD\nEND_IF").Length);
        }

        [TestCase("IF_BYTE 1 == 1\nEND_IF",               0)]
        [TestCase("IF_BYTE 1 == 1\nYIELD\nEND_IF",        2)]
        [TestCase("IF_BYTE 1 == 1\nYIELD\nRETURN\nEND_IF", 4)]
        public void ThenItJumpsPastEveryInstructionInItsBody(string source, int distance)
        {
            Assert.AreEqual(distance, BitConverter.ToInt32(FieldScriptCompiler.Compile(source), 6));
        }

        [Test]
        public void ThenAnIfInsideIsJumpedWhole()
        {
            byte[] nested = FieldScriptCompiler.Compile("IF_BYTE 1 == 1\nIF_BYTE 2 == 2\nYIELD\nEND_IF\nEND_IF");

            Assert.AreEqual(12, BitConverter.ToInt32(nested, 6));
        }

        [Test]
        public void ThenAnIfInsideKeepsItsOwnDistance()
        {
            byte[] nested = FieldScriptCompiler.Compile("IF_BYTE 1 == 1\nIF_BYTE 2 == 2\nYIELD\nEND_IF\nEND_IF");

            Assert.AreEqual(2, BitConverter.ToInt32(nested, 16));
        }

        [Test]
        public void ThenABodyPast65535BytesCompiles()
        {
            string source = "IF_BYTE 1 == 1\n" + string.Concat(Enumerable.Repeat("YIELD\n", 40000)) + "END_IF";

            Assert.AreEqual(80000, BitConverter.ToInt32(FieldScriptCompiler.Compile(source), 6));
        }

        [Test]
        public void ThenABoolIsOneByte()
        {
            byte[] ifBool = FieldScriptCompiler.Compile("IF_BOOL true == false\nYIELD\nEND_IF");

            Assert.AreEqual(12, ifBool.Length);
            CollectionAssert.AreEqual(new byte[] { 1, 0 }, new[] { ifBool[3], ifBool[4] });
        }

        [Test]
        public void ThenAnIntsValuesAreFourBytesEach()
        {
            byte[] ifInt = FieldScriptCompiler.Compile("IF_INT -1 < 70000\nEND_IF");

            Assert.AreEqual(16, ifInt.Length);
            CollectionAssert.AreEqual(new[] { -1, 70000 }, new[] { BitConverter.ToInt32(ifInt, 3), BitConverter.ToInt32(ifInt, 7) });
            Assert.AreEqual((byte)ScriptComparison.LessThan, ifInt[11]);
        }

        [Test]
        public void ThenANegativeSByteIsItsByte()
        {
            byte[] ifSByte = FieldScriptCompiler.Compile("IF_SBYTE -1 < 1\nEND_IF");

            Assert.AreEqual(10, ifSByte.Length);
            Assert.AreEqual(0xFF, ifSByte[3]);
        }

        [Test]
        public void ThenAFloatsValuesAreFloats()
        {
            byte[] ifFloat = FieldScriptCompiler.Compile("IF_FLOAT 1.5 >= 0.25\nEND_IF");

            Assert.AreEqual(16, ifFloat.Length);
            CollectionAssert.AreEqual(new[] { 1.5f, 0.25f }, new[] { BitConverter.ToSingle(ifFloat, 3), BitConverter.ToSingle(ifFloat, 7) });
        }
    }
}
