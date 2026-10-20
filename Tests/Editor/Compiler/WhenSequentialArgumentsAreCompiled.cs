using System;
using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Compiler
{
    /// <summary>A sources byte comes before each pair of arguments that could come from a variable.</summary>
    internal sealed class WhenSequentialArgumentsAreCompiled
    {
        private byte[] m_Position;

        [SetUp]
        public void SetUp()
        {
            m_Position = FieldScriptCompiler.Compile("SET_ENTITY_POSITION 1 2 3");
        }

        [Test]
        public void ThenEachPairHasASourcesByte()
        {
            Assert.AreEqual(16, m_Position.Length);
            Assert.AreEqual(0, m_Position[2]);
            Assert.AreEqual(0, m_Position[11]);
        }

        [Test]
        public void ThenTheArgumentsKeepTheirOrderAroundThem()
        {
            CollectionAssert.AreEqual(new[] { 1f, 2f, 3f },
                                      new[] { BitConverter.ToSingle(m_Position, 3), BitConverter.ToSingle(m_Position, 7), BitConverter.ToSingle(m_Position, 12) });
        }

        [Test]
        public void ThenASeedIsASourcesByteAndAnInt()
        {
            byte[] seed = FieldScriptCompiler.Compile("RANDOM_SEED 7");

            Assert.AreEqual(7, seed.Length);
            Assert.AreEqual(0, seed[2]);
            Assert.AreEqual(7, BitConverter.ToInt32(seed, 3));
        }
    }
}
