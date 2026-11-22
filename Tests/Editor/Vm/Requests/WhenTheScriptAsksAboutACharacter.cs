using NUnit.Framework;
using RPGFramework.Core.Memory;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>A character in the party's second slot, who has joined, and one who is neither, asked about into variables.</summary>
    internal sealed class WhenTheScriptAsksAboutACharacter
    {
        private const ulong  IN_PARTY = 0x1111;
        private const ulong  OUTSIDE  = 0x2222;
        private const ushort RESULT   = 40;

        private TestField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();

            m_Field.Vm.GetPartySlot       = character => character == IN_PARTY ? 1 : -1;
            m_Field.Vm.HasCharacterJoined = character => character == IN_PARTY;
        }

        [Test]
        public void ThenTheirSlotIsStored()
        {
            Run(FieldScriptOpCode.GetPartySlot, IN_PARTY);

            Assert.AreEqual(1, m_Field.Memory.ReadByte(MemoryBank.Persistent, RESULT));
        }

        [Test]
        public void ThenOneOutsideThePartyStoresAnEmptySlot()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, RESULT, 7);

            Run(FieldScriptOpCode.GetPartySlot, OUTSIDE);

            Assert.AreEqual(CoreVariables.EMPTY_PARTY_SLOT, m_Field.Memory.ReadByte(MemoryBank.Persistent, RESULT));
        }

        [Test]
        public void ThenWhetherTheyHaveJoinedIsStored()
        {
            Run(FieldScriptOpCode.GetCharacterJoined, IN_PARTY);

            Assert.IsTrue(m_Field.Memory.ReadBool(MemoryBank.Persistent, RESULT));
        }

        [Test]
        public void ThenOneWhoHasNotJoinedIsStoredAsNot()
        {
            m_Field.Memory.WriteBool(MemoryBank.Persistent, RESULT, true);

            Run(FieldScriptOpCode.GetCharacterJoined, OUTSIDE);

            Assert.IsFalse(m_Field.Memory.ReadBool(MemoryBank.Persistent, RESULT));
        }

        private void Run(FieldScriptOpCode opCode, ulong character)
        {
            m_Field.Run(new Script().Op(opCode).ULong(character).Sources(Source.Persistent, Source.Immediate).UShort(RESULT).Return());
        }
    }
}
