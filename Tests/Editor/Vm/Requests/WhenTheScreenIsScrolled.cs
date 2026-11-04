using NUnit.Framework;
using RPGFramework.Core.SharedTypes;
using RPGFramework.Field.FieldVmArgs;
using UnityEngine;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>A scroll to a point and a scroll onto an entity, each with its seconds and easing.</summary>
    internal sealed class WhenTheScreenIsScrolled
    {
        private const ushort MARK = 1000;

        private TestField m_Field;
        private Vector3   m_Point;
        private int       m_EntityId;
        private float     m_PointSeconds;
        private float     m_EntitySeconds;
        private Easing    m_PointEasing;
        private Easing    m_EntityEasing;

        [SetUp]
        public void SetUp()
        {
            m_Field         = new TestField();
            m_Point         = Vector3.zero;
            m_EntityId      = FieldEntity.NO_ENTITY;
            m_PointSeconds  = 0f;
            m_EntitySeconds = 0f;
            m_PointEasing   = Easing.Linear;
            m_EntityEasing  = Easing.Linear;

            m_Field.Memory.WriteFloat(MemoryBank.Persistent, 50, 1.5f);
            m_Field.Vm.RequestScrollScreenToPosition += OnScrollToPosition;
            m_Field.Vm.RequestScrollScreenToEntity   += OnScrollToEntity;

            m_Field.Run(new Script().Op(FieldScriptOpCode.ScrollScreenToPosition)
                                    .Sources(Source.Immediate, Source.Immediate).Float(1f).Float(2f)
                                    .Sources(Source.Immediate, Source.Persistent).Float(3f).UShort(50)
                                    .Sources(Source.Immediate, Source.Immediate).Byte((byte)Easing.Smooth)
                                    .Op(FieldScriptOpCode.ScrollScreenToEntity)
                                    .Sources(Source.Immediate, Source.Immediate).Byte(7).Float(2f)
                                    .Sources(Source.Immediate, Source.Immediate).Byte((byte)Easing.Linear)
                                    .SetByte(MARK, 1).Return());
        }

        [Test]
        public void ThenItAsksForThePoint()
        {
            Assert.AreEqual(new Vector3(1f, 2f, 3f), m_Point);
        }

        [Test]
        public void ThenAPointsSecondsCanComeFromAVariable()
        {
            Assert.AreEqual(1.5f, m_PointSeconds);
        }

        [Test]
        public void ThenItAsksForThePointsEasing()
        {
            Assert.AreEqual(Easing.Smooth, m_PointEasing);
        }

        [Test]
        public void ThenItAsksForTheEntity()
        {
            Assert.AreEqual(7, m_EntityId);
            Assert.AreEqual(2f, m_EntitySeconds);
            Assert.AreEqual(Easing.Linear, m_EntityEasing);
        }

        [Test]
        public void ThenItsArgumentsAreReadExactly()
        {
            Assert.AreEqual(1, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }

        private void OnScrollToPosition(Vector3 point, float seconds, Easing easing)
        {
            m_Point        = point;
            m_PointSeconds = seconds;
            m_PointEasing  = easing;
        }

        private void OnScrollToEntity(int entityId, float seconds, Easing easing)
        {
            m_EntityId      = entityId;
            m_EntitySeconds = seconds;
            m_EntityEasing  = easing;
        }
    }
}
