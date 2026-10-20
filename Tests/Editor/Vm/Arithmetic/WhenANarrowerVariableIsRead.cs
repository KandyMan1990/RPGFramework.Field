using NUnit.Framework;
using RPGFramework.Core.Memory;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Arithmetic
{
    /// <summary>A narrower variable read into a wider one is converted as a cast would convert it.</summary>
    internal sealed class WhenANarrowerVariableIsRead
    {
        private TestField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();
        }

        [Test]
        public void ThenAnIntFromAByteKeepsItsValue()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 20, 200);

            m_Field.Run(new Script().Op(FieldScriptOpCode.SetInt).Sources(Source.Persistent, Source.Persistent).UShort(120)
                                    .Variable(VariableWidth.Byte, 20).Return());

            Assert.AreEqual(200, m_Field.Memory.ReadInt(MemoryBank.Persistent, 120));
        }

        [Test]
        public void ThenAnIntFromAnSByteExtendsItsSign()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 20, 200);

            m_Field.Run(new Script().Op(FieldScriptOpCode.SetInt).Sources(Source.Persistent, Source.Persistent).UShort(120)
                                    .Variable(VariableWidth.SByte, 20).Return());

            Assert.AreEqual(-56, m_Field.Memory.ReadInt(MemoryBank.Persistent, 120));
        }

        [Test]
        public void ThenAULongFromAnSByteOfMinusOneHasEveryBitSet()
        {
            m_Field.Memory.WriteByte(MemoryBank.Persistent, 20, 0xFF);

            m_Field.Run(new Script().Op(FieldScriptOpCode.SetULong).Sources(Source.Persistent, Source.Persistent).UShort(100)
                                    .Variable(VariableWidth.SByte, 20).Return());

            Assert.AreEqual(ulong.MaxValue, m_Field.Memory.ReadUlong(MemoryBank.Persistent, 100));
        }

        [Test]
        public void ThenAFloatFromAnIntKeepsItsValue()
        {
            m_Field.Memory.WriteInt(MemoryBank.Persistent, 120, -7);

            m_Field.Run(new Script().Op(FieldScriptOpCode.SetFloat).Sources(Source.Persistent, Source.Persistent).UShort(110)
                                    .Variable(VariableWidth.Int, 120).Return());

            Assert.AreEqual(-7f, m_Field.Memory.ReadFloat(MemoryBank.Persistent, 110));
        }

        [Test]
        public void ThenAnAdditionToAnIntReadsAUShortAsUnsigned()
        {
            m_Field.Memory.WriteInt(MemoryBank.Persistent, 120, -7);
            m_Field.Memory.WriteUshort(MemoryBank.Persistent, 40, 65535);

            m_Field.Run(new Script().Op(FieldScriptOpCode.AddInt).Sources(Source.Persistent, Source.Persistent).UShort(120)
                                    .Variable(VariableWidth.UShort, 40).Return());

            Assert.AreEqual(65528, m_Field.Memory.ReadInt(MemoryBank.Persistent, 120));
        }
    }
}
