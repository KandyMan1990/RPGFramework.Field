using NUnit.Framework;
using RPGFramework.Field.Tests.Vm;

namespace RPGFramework.Field.Tests.Entities
{
    /// <summary>
    /// Four entities: a costume of the first character left out of the field, the same character's other costume, the
    /// second character, and an entity playing no one.
    /// </summary>
    internal sealed class WhenTheEntityPlayingACharacterIsFound
    {
        private const ulong FIRST  = 0x1111;
        private const ulong SECOND = 0x2222;

        private FieldContext m_Context;

        [SetUp]
        public void SetUp()
        {
            FieldEntityRuntime[] entities =
            {
                new FieldEntityRuntime(1, new[] { 100 }, new byte[1]),
                new FieldEntityRuntime(2, new[] { 101 }, new byte[1]),
                new FieldEntityRuntime(3, new[] { 102 }, new byte[1]),
                new FieldEntityRuntime(4, new[] { 103 }, new byte[1])
            };

            m_Context = new FieldContext(new TestField().Vm, entities, new byte[0], new byte[0], 0);

            m_Context.EntityState(1).Character = FIRST;
            m_Context.EntityState(1).SetInField(false);
            m_Context.EntityState(2).Character = FIRST;
            m_Context.EntityState(3).Character = SECOND;
        }

        [Test]
        public void ThenTheFirstStillInTheFieldPlaysIt()
        {
            Assert.AreEqual(2, m_Context.FindEntityPlaying(FIRST).EntityId);
            Assert.AreEqual(3, m_Context.FindEntityPlaying(SECOND).EntityId);
        }

        [Test]
        public void ThenACharacterNoEntityPlaysHasNone()
        {
            Assert.IsNull(m_Context.FindEntityPlaying(0x3333));
        }

        [Test]
        public void ThenNoCharacterHasNone()
        {
            Assert.IsNull(m_Context.FindEntityPlaying(0));
        }
    }
}
