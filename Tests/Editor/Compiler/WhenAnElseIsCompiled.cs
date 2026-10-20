using System;
using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Compiler
{
    /// <summary>
    /// An IF (10 bytes), its body, a jump (6) and the ELSE body: a failed comparison jumps past the IF body and the
    /// jump, and the jump past the ELSE body.
    /// </summary>
    internal sealed class WhenAnElseIsCompiled
    {
        private byte[] m_IfElse;

        [SetUp]
        public void SetUp()
        {
            m_IfElse = FieldScriptCompiler.Compile("IF_BYTE 1 == 1\nYIELD\nELSE\nRETURN\nEND_IF");
        }

        [Test]
        public void ThenItIsTheComparisonTheBodyAJumpAndTheElseBody()
        {
            Assert.AreEqual(20, m_IfElse.Length);
        }

        [Test]
        public void ThenAFailedComparisonJumpsPastTheBodyAndTheJump()
        {
            Assert.AreEqual(8, BitConverter.ToInt32(m_IfElse, 6));
        }

        [Test]
        public void ThenTheBodyEndsInAJump()
        {
            Assert.AreEqual((ushort)FieldScriptOpCode.GotoJump, BitConverter.ToUInt16(m_IfElse, 12));
        }

        [Test]
        public void ThenTheJumpGoesPastTheElseBody()
        {
            Assert.AreEqual(2, BitConverter.ToInt32(m_IfElse, 14));
        }

        [Test]
        public void ThenAnEmptyElseJumpsNowhere()
        {
            Assert.AreEqual(0, BitConverter.ToInt32(FieldScriptCompiler.Compile("IF_BYTE 1 == 1\nYIELD\nELSE\nEND_IF"), 14));
        }

        [Test]
        public void ThenAnIfInsideTheElseIsJumpedWhole()
        {
            byte[] ifInElse = FieldScriptCompiler.Compile("IF_BYTE 1 == 1\nYIELD\nELSE\nIF_BYTE 2 == 2\nRETURN\nEND_IF\nEND_IF");

            Assert.AreEqual(12, BitConverter.ToInt32(ifInElse, 14));
        }
    }
}
