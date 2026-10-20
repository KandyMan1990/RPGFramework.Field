using NUnit.Framework;
using RPGFramework.Core.Memory;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Arithmetic
{
    internal sealed class WhenARandomNumberIsTaken
    {
        private const ushort ROLL = 10;
        private const ushort MARK = 1000;

        private TestField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();
        }

        [Test]
        public void ThenItReadsOnlyWhereItGoes()
        {
            m_Field.Run(Roll(new Script()).SetByte(MARK, 1).Return());

            Assert.AreEqual(1, m_Field.Memory.ReadByte(MemoryBank.Persistent, MARK));
        }

        [Test]
        public void ThenEveryByteCanBeRolled()
        {
            bool rolledHigh = false;

            for (int i = 0; i < 400 && !rolledHigh; i++)
            {
                m_Field.Run(Roll(new Script()).Return());

                rolledHigh = m_Field.Memory.ReadByte(MemoryBank.Persistent, ROLL) >= 128;
            }

            Assert.IsTrue(rolledHigh);
        }

        [Test]
        public void ThenTheSameSeedRollsTheSameNumber()
        {
            Script seeded = Roll(new Script().Op(FieldScriptOpCode.RandomNumberSeed).Sources(Source.Immediate, Source.Immediate).Int(7)).Return();

            m_Field.Run(seeded);
            byte first = m_Field.Memory.ReadByte(MemoryBank.Persistent, ROLL);
            m_Field.Run(Roll(new Script()).Return());
            m_Field.Run(seeded);

            Assert.AreEqual(first, m_Field.Memory.ReadByte(MemoryBank.Persistent, ROLL));
        }

        [Test]
        public void ThenASeedFromAVariableSeedsAsTheSameImmediate()
        {
            m_Field.Run(Roll(new Script().Op(FieldScriptOpCode.RandomNumberSeed).Sources(Source.Immediate, Source.Immediate).Int(7)).Return());
            byte immediate = m_Field.Memory.ReadByte(MemoryBank.Persistent, ROLL);
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 20, 7);

            m_Field.Run(Roll(new Script().Op(FieldScriptOpCode.RandomNumberSeed).Sources(Source.Immediate, Source.Persistent)
                                         .Variable(VariableWidth.Byte, 20)).Return());

            Assert.AreEqual(immediate, m_Field.Memory.ReadByte(MemoryBank.Persistent, ROLL));
        }

        private static Script Roll(Script script)
        {
            return script.Op(FieldScriptOpCode.GetRandomNumber).Sources(Source.Persistent, Source.Immediate).UShort(ROLL);
        }
    }
}
