using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>An entity names the three animations it climbs with.</summary>
    internal sealed class WhenAnEntitysLadderAnimationsAreNamed
    {
        private const ulong GET_ON  = 11;
        private const ulong CLIMB   = 22;
        private const ulong GET_OFF = 33;

        private TestField          m_Field;
        private FieldEntityRuntime m_Entity;
        private int                m_Named;
        private ulong              m_GetOn;
        private ulong              m_Climb;
        private ulong              m_GetOff;

        [SetUp]
        public void SetUp()
        {
            m_Field  = new TestField();
            m_Named  = -1;
            m_GetOn  = 0;
            m_Climb  = 0;
            m_GetOff = 0;

            m_Field.Vm.RequestSetLadderAnimations += OnNamed;

            m_Entity = m_Field.Run(new Script().Op(FieldScriptOpCode.SetLadderAnimations).ULong(GET_ON).ULong(CLIMB).ULong(GET_OFF).Return());
        }

        [Test]
        public void ThenItIsThisEntitysAnimations()
        {
            Assert.AreEqual(m_Entity.EntityId, m_Named);
        }

        [Test]
        public void ThenEachIsPassedOnInItsPlace()
        {
            Assert.AreEqual(GET_ON, m_GetOn);
            Assert.AreEqual(CLIMB, m_Climb);
            Assert.AreEqual(GET_OFF, m_GetOff);
        }

        private void OnNamed(int entityId, ulong getOn, ulong climb, ulong getOff)
        {
            m_Named  = entityId;
            m_GetOn  = getOn;
            m_Climb  = climb;
            m_GetOff = getOff;
        }
    }
}
