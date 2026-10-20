using NUnit.Framework;
using RPGFramework.Core.SharedTypes;
using UnityEngine;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>
    /// A sequential opcode's arguments can each come from a bank, read as an address alone. A pair shares a sources
    /// byte, so the argument after a pair reads a new one.
    /// </summary>
    internal sealed class WhenAnArgumentComesFromAVariable
    {
        private const ushort MARK = 1000;

        private TestField m_Field;
        private float     m_Range;
        private Vector3   m_Position;

        [SetUp]
        public void SetUp()
        {
            m_Field    = new TestField();
            m_Range    = 0f;
            m_Position = default;

            m_Field.Memory.WriteFloat(MemoryBank.Persistent, 50, 2.5f);
            m_Field.Memory.WriteFloat(MemoryBank.Session, 60, 7.25f);

            m_Field.Vm.RequestSetInteractionRange += OnInteractionRange;
            m_Field.Vm.RequestSetEntityPosition   += OnPosition;
        }

        [Test]
        public void ThenItIsReadFromTheBankItNames()
        {
            m_Field.Run(Range().Return());

            Assert.AreEqual(2.5f, m_Range);
        }

        [Test]
        public void ThenItIsASourcesByteAndAnAddress()
        {
            m_Field.Run(Range().SetByte(MARK, 1).Return());

            Assert.AreEqual(1, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }

        [Test]
        public void ThenAValueAndAVariableShareASourcesByte()
        {
            m_Field.Run(Position().Return());

            Assert.AreEqual(new Vector2(1f, 7.25f), new Vector2(m_Position.x, m_Position.y));
        }

        [Test]
        public void ThenTheArgumentAfterAPairReadsANewSourcesByte()
        {
            m_Field.Run(Position().Return());

            Assert.AreEqual(3f, m_Position.z);
        }

        [Test]
        public void ThenThePairsArgumentsAreReadExactly()
        {
            m_Field.Run(Position().SetByte(MARK, 1).Return());

            Assert.AreEqual(1, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }

        private static Script Range()
        {
            return new Script().Op(FieldScriptOpCode.SetInteractionRange).Sources(Source.Persistent, Source.Immediate).UShort(50);
        }

        private static Script Position()
        {
            return new Script().Op(FieldScriptOpCode.SetEntityPosition).Sources(Source.Immediate, Source.Session).Float(1f).UShort(60)
                               .Sources(Source.Immediate, Source.Immediate).Float(3f);
        }

        private void OnInteractionRange(int entityId, float range)
        {
            m_Range = range;
        }

        private void OnPosition(int entityId, Vector3 position)
        {
            m_Position = position;
        }
    }
}
