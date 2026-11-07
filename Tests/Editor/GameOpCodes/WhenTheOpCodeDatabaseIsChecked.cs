using System.Collections.Generic;
using NUnit.Framework;

namespace RPGFramework.Field.Tests.GameOpCodes
{
    /// <summary>
    /// A game's database is code, which the editor cannot read before play, so it is checked against the game's tables
    /// as the field module enters.
    /// </summary>
    internal sealed class WhenTheOpCodeDatabaseIsChecked
    {
        private Dictionary<ushort, DescribedGameOpCode> m_Described;
        private List<string>                            m_Problems;

        [SetUp]
        public void SetUp()
        {
            m_Described = FieldOpCodeTables.DescribeGameOpCodes(new[] { typeof(TestGameOpCode) });
            m_Problems  = new List<string>();
        }

        [Test]
        public void ThenADatabaseAddingEveryOpCodeHasNoProblems()
        {
            Check(Every());

            CollectionAssert.IsEmpty(m_Problems);
        }

        [Test]
        public void ThenAnOpCodeDescribedButNotAddedIsReported()
        {
            Check(new TestOpCodeDatabase().With(TestGameOpCode.TakeTwo, Nothing).With(TestGameOpCode.Name, Nothing));

            Assert.That(m_Problems, Has.Some.Contains("TEST_WRITE (0xFF01) is described but not added"));
        }

        [Test]
        public void ThenAnOpCodeAddedButNotDescribedIsReported()
        {
            Check(Every().With((TestGameOpCode)0xFF7F, Nothing));

            Assert.That(m_Problems, Has.Some.Contains("0xFF7F is added with Nothing, but no [FieldOpCode]"));
        }

        [Test]
        public void ThenAnOpCodeAmongTheFrameworksIsReported()
        {
            Check(Every().With((TestGameOpCode)0x0100, Nothing));

            Assert.That(m_Problems, Has.Some.Contains("0x0100 is added with Nothing, but a game's opcodes start at 0x8000"));
        }

        private void Check(TestOpCodeDatabase database)
        {
            FieldGameOpCodeDatabaseCheck.Check(database, m_Described, m_Problems);
        }

        private static TestOpCodeDatabase Every()
        {
            return new TestOpCodeDatabase().With(TestGameOpCode.TakeTwo, Nothing).With(TestGameOpCode.Write, Nothing).With(TestGameOpCode.Name, Nothing);
        }

        private static void Nothing(FieldScriptContext context)
        {
        }
    }
}
