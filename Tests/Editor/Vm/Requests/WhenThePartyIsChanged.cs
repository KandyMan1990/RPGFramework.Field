using NUnit.Framework;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>Each opcode changing the party, followed by an instruction marking that the script went on.</summary>
    internal sealed class WhenThePartyIsChanged
    {
        private const ulong  FIRST  = 0x1111;
        private const ulong  SECOND = 0x2222;
        private const ushort MARK   = 40;

        private TestField m_Field;
        private ulong     m_Character;
        private ulong[]   m_Characters;
        private bool      m_Joined;
        private string    m_Asked;

        [SetUp]
        public void SetUp()
        {
            m_Field      = new TestField();
            m_Character  = 0;
            m_Characters = null;
            m_Joined     = false;
            m_Asked      = null;

            m_Field.Vm.RequestAddToParty         += character => Ask("add", character);
            m_Field.Vm.RequestRemoveFromParty    += character => Ask("remove", character);
            m_Field.Vm.RequestSetParty           += characters => { m_Asked = "set"; m_Characters = characters; };
            m_Field.Vm.RequestSetCharacterJoined += (character, joined) => { Ask("join", character); m_Joined = joined; };
        }

        [Test]
        public void ThenAddingAsksForTheCharacter()
        {
            Run(new Script().Op(FieldScriptOpCode.AddToParty).ULong(FIRST));

            Assert.AreEqual("add", m_Asked);
            Assert.AreEqual(FIRST, m_Character);
        }

        [Test]
        public void ThenRemovingAsksForTheCharacter()
        {
            Run(new Script().Op(FieldScriptOpCode.RemoveFromParty).ULong(SECOND));

            Assert.AreEqual("remove", m_Asked);
            Assert.AreEqual(SECOND, m_Character);
        }

        [Test]
        public void ThenSettingAsksForEachInOrder()
        {
            Run(new Script().Op(FieldScriptOpCode.SetParty).Byte(2).ULong(SECOND).ULong(FIRST));

            Assert.AreEqual("set", m_Asked);
            CollectionAssert.AreEqual(new[] { SECOND, FIRST }, m_Characters);
        }

        [Test]
        public void ThenLeavingAsksForTheCharacterNotJoined()
        {
            m_Joined = true;

            Run(new Script().Op(FieldScriptOpCode.SetCharacterJoined).ULong(FIRST).Sources(Source.Immediate, Source.Immediate).Bool(false));

            Assert.AreEqual("join", m_Asked);
            Assert.AreEqual(FIRST, m_Character);
            Assert.IsFalse(m_Joined);
        }

        private void Ask(string what, ulong character)
        {
            m_Asked     = what;
            m_Character = character;
        }

        private void Run(Script script)
        {
            m_Field.Run(script.SetByte(MARK, 1).Return());

            Assert.AreEqual(1, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK), "the instruction after it ran");
        }
    }
}
