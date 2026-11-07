using System;
using NUnit.Framework;
using RPGFramework.Field.Editor;
using RPGFramework.Field.Tests.Vm;

namespace RPGFramework.Field.Tests.GameOpCodes
{
    /// <summary>A game's opcode compiles from its attributes, as the framework's sequential opcodes do.</summary>
    internal sealed class WhenAGameOpCodeIsCompiled
    {
        private FieldOpCodeCatalogue m_Catalogue;

        [SetUp]
        public void SetUp()
        {
            m_Catalogue = new FieldOpCodeCatalogue(new[] { typeof(FieldScriptOpCode), typeof(TestGameOpCode) });
        }

        [Test]
        public void ThenItIsWrittenAsTheFrameworksOpCodesAre()
        {
            byte[] expected = new Script().UShort((ushort)TestGameOpCode.TakeTwo).Sources(Source.Immediate, Source.Immediate).Byte(7).Int(1234).ToArray();

            CollectionAssert.AreEqual(expected, FieldScriptCompiler.Compile("TEST_TAKE_TWO 7 1234", m_Catalogue));
        }

        [Test]
        public void ThenADestinationWrittenAsANumberIsRefused()
        {
            Exception refused = Assert.Throws<Exception>(() => FieldScriptCompiler.Compile("TEST_WRITE 5 6", m_Catalogue));

            StringAssert.Contains("writes its result to a variable", refused.Message);
        }

        [Test]
        public void ThenTheProjectsCatalogueLeavesATestsTableOut()
        {
            Assert.IsFalse(FieldOpCodeCatalogue.Project.TryGet("TEST_TAKE_TWO", out _));
        }
    }
}
