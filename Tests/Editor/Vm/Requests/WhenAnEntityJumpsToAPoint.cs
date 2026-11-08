using NUnit.Framework;
using RPGFramework.Core.SharedTypes;
using UnityEngine;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>An entity jumps to (1, 2, 3) over two seconds, peaking 0.5 up, and is in the air unless a test says otherwise.</summary>
    internal sealed class WhenAnEntityJumpsToAPoint
    {
        private const ushort MARK = 1000;

        private TestField          m_Field;
        private FieldEntityRuntime m_Entity;
        private bool               m_Jumping;
        private int                m_Jumper;
        private Vector3            m_Target;
        private float              m_Seconds;
        private float              m_Height;

        [SetUp]
        public void SetUp()
        {
            m_Field   = new TestField();
            m_Jumping = true;
            m_Jumper  = -1;
            m_Target  = default;
            m_Seconds = 0f;
            m_Height  = 0f;

            m_Field.Vm.IsEntityJumping   =  IsJumping;
            m_Field.Vm.RequestJumpEntity += OnJump;

            m_Entity = m_Field.Run(new Script().Op(FieldScriptOpCode.JumpEntityAndWait)
                                               .Sources(Source.Immediate, Source.Immediate).Float(1f).Float(2f)
                                               .Sources(Source.Immediate, Source.Immediate).Float(3f).Float(2f)
                                               .Sources(Source.Immediate, Source.Immediate).Float(0.5f)
                                               .SetByte(MARK, 1).Return());
        }

        [Test]
        public void ThenItIsThisEntityThatJumps()
        {
            Assert.AreEqual(m_Entity.EntityId, m_Jumper);
        }

        [Test]
        public void ThenItJumpsToThePointGiven()
        {
            Assert.AreEqual(new Vector3(1f, 2f, 3f), m_Target);
        }

        [Test]
        public void ThenItTakesTheTimeGiven()
        {
            Assert.AreEqual(2f, m_Seconds);
        }

        [Test]
        public void ThenItPeaksAtTheHeightGiven()
        {
            Assert.AreEqual(0.5f, m_Height);
        }

        [Test]
        public void ThenTheScriptWaitsWhileItIsInTheAir()
        {
            m_Field.Tick(m_Entity, 3);

            Assert.AreEqual(0, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }

        [Test]
        public void ThenTheScriptCarriesOnOnceItHasLanded()
        {
            m_Jumping = false;
            m_Field.Tick(m_Entity, 2);

            Assert.AreEqual(1, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }

        private bool IsJumping(int entityId)
        {
            return m_Jumping;
        }

        private void OnJump(int entityId, Vector3 target, float seconds, float height)
        {
            m_Jumper  = entityId;
            m_Target  = target;
            m_Seconds = seconds;
            m_Height  = height;
        }
    }
}
