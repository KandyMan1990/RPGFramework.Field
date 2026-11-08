using NUnit.Framework;
using RPGFramework.Core.SharedTypes;
using UnityEngine;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>A script asks about entity 7, which stands at (1, 2, 3) and faces 90 degrees.</summary>
    internal sealed class WhenAScriptReadsWhereAnEntityIs
    {
        private const byte   ENTITY    = 7;
        private const float  DIRECTION = 90f;
        private const ushort X         = 100;
        private const ushort Y         = 104;
        private const ushort Z         = 108;
        private const ushort FACING    = 112;

        private TestField m_Field;
        private int       m_Asked;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();
            m_Asked = -1;

            m_Field.Vm.GetEntityPosition  = PositionOf;
            m_Field.Vm.GetEntityDirection = DirectionOf;
        }

        [Test]
        public void ThenItsPositionIsStored()
        {
            ReadPosition();

            Assert.AreEqual(new Vector3(1f, 2f, 3f), new Vector3(Stored(X), Stored(Y), Stored(Z)));
        }

        [Test]
        public void ThenThePositionAskedForIsTheEntityNamed()
        {
            ReadPosition();

            Assert.AreEqual(ENTITY, m_Asked);
        }

        [Test]
        public void ThenItsDirectionIsStored()
        {
            ReadDirection();

            Assert.AreEqual(DIRECTION, Stored(FACING));
        }

        [Test]
        public void ThenTheDirectionAskedForIsTheEntityNamed()
        {
            ReadDirection();

            Assert.AreEqual(ENTITY, m_Asked);
        }

        private void ReadPosition()
        {
            m_Field.Run(new Script().Op(FieldScriptOpCode.GetEntityPosition)
                                    .Sources(Source.Immediate, Source.Persistent).Byte(ENTITY).UShort(X)
                                    .Sources(Source.Persistent, Source.Persistent).UShort(Y).UShort(Z).Return());
        }

        private void ReadDirection()
        {
            m_Field.Run(new Script().Op(FieldScriptOpCode.GetEntityDirection)
                                    .Sources(Source.Immediate, Source.Persistent).Byte(ENTITY).UShort(FACING).Return());
        }

        private float Stored(ushort address)
        {
            return m_Field.Memory.ReadFloat(MemoryBank.Persistent, address);
        }

        private Vector3 PositionOf(int entityId)
        {
            m_Asked = entityId;

            return new Vector3(1f, 2f, 3f);
        }

        private float DirectionOf(int entityId)
        {
            m_Asked = entityId;

            return DIRECTION;
        }
    }
}
