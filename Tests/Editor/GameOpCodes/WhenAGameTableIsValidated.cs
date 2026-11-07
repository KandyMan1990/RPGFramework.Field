using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.GameOpCodes
{
    /// <summary>Export checks a game's table as it checks the framework's, and against what only a game's may be.</summary>
    internal sealed class WhenAGameTableIsValidated
    {
        private string[] m_Problems;

        [SetUp]
        public void SetUp()
        {
            m_Problems = new FieldOpCodeCatalogue(new[] { typeof(FieldScriptOpCode), typeof(InvalidGameOpCode), typeof(WideGameOpCode) }).Validate();
        }

        [Test]
        public void ThenAGoodTableHasNoProblems()
        {
            CollectionAssert.IsEmpty(new FieldOpCodeCatalogue(new[] { typeof(FieldScriptOpCode), typeof(TestGameOpCode) }).Validate());
        }

        [Test]
        public void ThenAValueAmongTheFrameworksIsRefused()
        {
            Assert.That(m_Problems, Has.Some.Contains("'InvalidGameOpCode.Low' is 0x7F00, among the framework's opcodes"));
        }

        [Test]
        public void ThenTwoOpCodesSharingAValueAreRefused()
        {
            Assert.That(m_Problems, Has.Some.Contains("are both 0xFE01"));
        }

        [Test]
        public void ThenAFrameworkScriptNameIsRefused()
        {
            Assert.That(m_Problems, Has.Some.Contains("both use the scriptName RETURN"));
        }

        [Test]
        public void ThenACompilerWordIsRefused()
        {
            Assert.That(m_Problems, Has.Some.Contains("LABEL, which the compiler reads as its own word"));
        }

        [Test]
        public void ThenAnotherLayoutIsRefused()
        {
            Assert.That(m_Problems, Has.Some.Contains("'InvalidGameOpCode.Bank' is BankUnary: a game's opcodes are Sequential"));
        }

        [Test]
        public void ThenABlockIsRefused()
        {
            Assert.That(m_Problems, Has.Some.Contains("'InvalidGameOpCode.Block' opens a block"));
        }

        [Test]
        public void ThenAnArgumentAHandlerCannotReadIsRefused()
        {
            Assert.That(m_Problems, Has.Some.Contains("is a Label, which a game's opcode handler cannot read"));
        }

        [Test]
        public void ThenADestinationOfAnotherWidthIsRefused()
        {
            Assert.That(m_Problems, Has.Some.Contains("is a Long variable"));
        }

        [Test]
        public void ThenATableNotOfUShortsIsRefused()
        {
            Assert.That(m_Problems, Has.Some.Contains("WideGameOpCode describes opcodes but is not a ushort enum"));
        }
    }
}
