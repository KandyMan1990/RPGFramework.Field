using NUnit.Framework;
using RPGFramework.Core.SharedTypes;
using RPGFramework.Field.FieldVmArgs;
using UnityEngine;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>An entity's draw offset goes to (1, 2, 3) over two seconds, easing, and is moving unless a test says otherwise.</summary>
    internal sealed class WhenAnEntitysDrawOffsetIsSet
    {
        private const ushort MARK = 1000;

        private TestField m_Field;
        private bool      m_Moving;
        private int       m_Entity;
        private Vector3   m_Offset;
        private float     m_Seconds;
        private Easing    m_Easing;

        [SetUp]
        public void SetUp()
        {
            m_Field   = new TestField();
            m_Moving  = true;
            m_Entity  = -1;
            m_Offset  = default;
            m_Seconds = 0f;
            m_Easing  = Easing.Linear;

            m_Field.Vm.IsEntityDrawOffsetMoving   =  IsMoving;
            m_Field.Vm.RequestSetEntityDrawOffset += OnOffset;
        }

        [Test]
        public void ThenItIsThisEntitysOffset()
        {
            FieldEntityRuntime entity = m_Field.Run(Set(FieldScriptOpCode.SetEntityDrawOffset).Return());

            Assert.AreEqual(entity.EntityId, m_Entity);
        }

        [Test]
        public void ThenItGoesToTheOffsetGiven()
        {
            m_Field.Run(Set(FieldScriptOpCode.SetEntityDrawOffset).Return());

            Assert.AreEqual(new Vector3(1f, 2f, 3f), m_Offset);
        }

        [Test]
        public void ThenItTakesTheTimeGiven()
        {
            m_Field.Run(Set(FieldScriptOpCode.SetEntityDrawOffset).Return());

            Assert.AreEqual(2f, m_Seconds);
        }

        [Test]
        public void ThenItEasesAsAsked()
        {
            m_Field.Run(Set(FieldScriptOpCode.SetEntityDrawOffset).Return());

            Assert.AreEqual(Easing.Smooth, m_Easing);
        }

        [Test]
        public void ThenTheScriptCarriesOnAtOnce()
        {
            m_Field.Run(Set(FieldScriptOpCode.SetEntityDrawOffset).SetByte(MARK, 1).Return());

            Assert.AreEqual(1, Mark());
        }

        [Test]
        public void ThenTheWaitingFormAsksForTheSameOffset()
        {
            m_Field.Run(Set(FieldScriptOpCode.SetEntityDrawOffsetAndWait).Return());

            Assert.AreEqual(new Vector3(1f, 2f, 3f), m_Offset);
        }

        [Test]
        public void ThenTheWaitingFormWaitsWhileItMoves()
        {
            FieldEntityRuntime entity = m_Field.Run(Set(FieldScriptOpCode.SetEntityDrawOffsetAndWait).SetByte(MARK, 1).Return());

            m_Field.Tick(entity, 3);

            Assert.AreEqual(0, Mark());
        }

        [Test]
        public void ThenTheWaitingFormCarriesOnOnceItHasMoved()
        {
            FieldEntityRuntime entity = m_Field.Run(Set(FieldScriptOpCode.SetEntityDrawOffsetAndWait).SetByte(MARK, 1).Return());

            m_Moving = false;
            m_Field.Tick(entity, 2);

            Assert.AreEqual(1, Mark());
        }

        [Test]
        public void ThenAWaitForItWaitsWhileItMoves()
        {
            FieldEntityRuntime entity = m_Field.Run(new Script().Op(FieldScriptOpCode.WaitForEntityDrawOffset).SetByte(MARK, 1).Return());

            m_Field.Tick(entity, 3);

            Assert.AreEqual(0, Mark());
        }

        [Test]
        public void ThenAWaitForItCarriesOnOnceItHasMoved()
        {
            FieldEntityRuntime entity = m_Field.Run(new Script().Op(FieldScriptOpCode.WaitForEntityDrawOffset).SetByte(MARK, 1).Return());

            m_Moving = false;
            m_Field.Tick(entity, 2);

            Assert.AreEqual(1, Mark());
        }

        private static Script Set(FieldScriptOpCode opCode)
        {
            return new Script().Op(opCode)
                               .Sources(Source.Immediate, Source.Immediate).Float(1f).Float(2f)
                               .Sources(Source.Immediate, Source.Immediate).Float(3f).Float(2f)
                               .Sources(Source.Immediate, Source.Immediate).Byte((byte)Easing.Smooth);
        }

        private byte Mark()
        {
            return m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK);
        }

        private bool IsMoving(int entityId)
        {
            return m_Moving;
        }

        private void OnOffset(int entityId, Vector3 offset, float seconds, Easing easing)
        {
            m_Entity  = entityId;
            m_Offset  = offset;
            m_Seconds = seconds;
            m_Easing  = easing;
        }
    }
}
