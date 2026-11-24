using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>An entity's script naming the character it plays.</summary>
    internal sealed class WhenAnEntityIsMadeACharacters
    {
        private const ulong CHARACTER = 0x1111;

        private TestField          m_Field;
        private FieldEntityRuntime m_Entity;
        private int                m_EntityId;
        private ulong              m_Character;

        [SetUp]
        public void SetUp()
        {
            m_Field     = new TestField();
            m_EntityId  = -1;
            m_Character = 0;

            m_Field.Vm.RequestSetPartyCharacter += (entityId, character) =>
                                                   {
                                                       m_EntityId  = entityId;
                                                       m_Character = character;
                                                   };

            m_Entity = m_Field.Run(new Script().Op(FieldScriptOpCode.SetPartyCharacter).ULong(CHARACTER).Return());
        }

        [Test]
        public void ThenItAsksForThatEntity()
        {
            Assert.AreEqual(m_Entity.EntityId, m_EntityId);
        }

        [Test]
        public void ThenItAsksForTheCharacter()
        {
            Assert.AreEqual(CHARACTER, m_Character);
        }
    }
}
