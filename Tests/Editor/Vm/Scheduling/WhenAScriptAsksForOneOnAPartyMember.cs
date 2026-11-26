using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Scheduling
{
    /// <summary>A script asking for a script of whoever plays party slot 1, which entity 2 plays, slot 2 empty.</summary>
    internal sealed class WhenAScriptAsksForOneOnAPartyMember
    {
        private const byte CALLER = 1;
        private const byte MEMBER = 2;

        private TestField          m_Field;
        private FieldEntityRuntime m_Member;
        private int                m_Requested;

        [SetUp]
        public void SetUp()
        {
            m_Field     = new TestField();
            m_Requested = m_Field.Add(new Script().Return());
            m_Member    = m_Field.AddEntity(MEMBER, new byte[] { 0, 1 }, m_Field.Add(new Script().Return()), m_Requested);

            m_Field.Vm.GetPartyMemberEntity = slot => slot == 1 ? MEMBER : FieldEntity.NO_ENTITY;
        }

        [Test]
        public void ThenTheMembersEntityHoldsTheScriptInItsSlot()
        {
            Ask(1);

            Assert.IsTrue(m_Member.HoldsScript(m_Requested, 1));
        }

        [Test]
        public void ThenAnEmptySlotGetsNothing()
        {
            Ask(2);

            Assert.IsFalse(m_Member.IsSlotOccupied(1));
        }

        private void Ask(byte slot)
        {
            int caller = m_Field.Add(new Script().Request(FieldScriptOpCode.RunPartyMemberScriptUnlessBusy, slot, 1).Return());

            m_Field.Tick(m_Field.AddEntity(CALLER, new byte[1], caller));
        }
    }
}
