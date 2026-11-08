using NUnit.Framework;
using RPGFramework.Core.SharedTypes;
using RPGFramework.Field.FieldVmArgs;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>An entity turns to face entity 7 over two seconds, easing, and is still turning unless a test says otherwise.</summary>
    internal sealed class WhenAnEntityTurnsToAnother
    {
        private const byte   TARGET   = 7;
        private const float  DURATION = 2f;
        private const ushort MARK     = 1000;

        private TestField          m_Field;
        private FieldEntityRuntime m_Entity;
        private bool               m_Turning;
        private int                m_Turner;
        private int                m_Target;
        private float              m_Duration;
        private Easing             m_Easing;

        [SetUp]
        public void SetUp()
        {
            m_Field    = new TestField();
            m_Turning  = true;
            m_Turner   = -1;
            m_Target   = -1;
            m_Duration = 0f;
            m_Easing   = Easing.Linear;

            m_Field.Vm.IsEntityRotating          =  IsRotating;
            m_Field.Vm.RequestTurnEntityToEntity += OnTurn;

            m_Entity = m_Field.Run(new Script().Op(FieldScriptOpCode.TurnToEntityAndWait)
                                               .Sources(Source.Immediate, Source.Immediate).Byte(TARGET).Float(DURATION)
                                               .Sources(Source.Immediate, Source.Immediate).Byte((byte)Easing.Smooth)
                                               .SetByte(MARK, 1).Return());
        }

        [Test]
        public void ThenItIsThisEntityThatTurns()
        {
            Assert.AreEqual(m_Entity.EntityId, m_Turner);
        }

        [Test]
        public void ThenItTurnsToTheEntityNamed()
        {
            Assert.AreEqual(TARGET, m_Target);
        }

        [Test]
        public void ThenItTakesTheTimeGiven()
        {
            Assert.AreEqual(DURATION, m_Duration);
        }

        [Test]
        public void ThenItEasesAsAsked()
        {
            Assert.AreEqual(Easing.Smooth, m_Easing);
        }

        [Test]
        public void ThenTheScriptWaitsWhileItTurns()
        {
            m_Field.Tick(m_Entity, 3);

            Assert.AreEqual(0, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }

        [Test]
        public void ThenTheScriptCarriesOnOnceItHasTurned()
        {
            m_Turning = false;
            m_Field.Tick(m_Entity, 2);

            Assert.AreEqual(1, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }

        private bool IsRotating(int entityId)
        {
            return m_Turning;
        }

        private void OnTurn(int entityId, int targetEntityId, float duration, Easing easing)
        {
            m_Turner   = entityId;
            m_Target   = targetEntityId;
            m_Duration = duration;
            m_Easing   = easing;
        }
    }
}
