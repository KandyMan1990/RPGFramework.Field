using NUnit.Framework;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>A script asks whether entity 7 is in its own entity's range, and is told what a test sets.</summary>
    internal sealed class WhenAScriptAsksIfAnEntityIsInRange
    {
        private const byte   OTHER  = 7;
        private const ushort RESULT = 100;

        private TestField m_Field;
        private bool      m_InRange;
        private int       m_Asker;
        private int       m_Other;

        [SetUp]
        public void SetUp()
        {
            m_Field   = new TestField();
            m_InRange = false;
            m_Asker   = -1;
            m_Other   = -1;

            m_Field.Vm.IsEntityInRange = IsInRange;
        }

        [Test]
        public void ThenInRangeIsStored()
        {
            m_InRange = true;
            Ask();

            Assert.IsTrue(m_Field.Memory.ReadBool(MemoryBank.Persistent, RESULT));
        }

        [Test]
        public void ThenOutOfRangeIsStored()
        {
            m_Field.Memory.WriteBool(MemoryBank.Persistent, RESULT, true);
            Ask();

            Assert.IsFalse(m_Field.Memory.ReadBool(MemoryBank.Persistent, RESULT));
        }

        [Test]
        public void ThenItAsksFromThisEntity()
        {
            FieldEntityRuntime entity = Ask();

            Assert.AreEqual(entity.EntityId, m_Asker);
        }

        [Test]
        public void ThenItAsksAboutTheEntityNamed()
        {
            Ask();

            Assert.AreEqual(OTHER, m_Other);
        }

        private FieldEntityRuntime Ask()
        {
            return m_Field.Run(new Script().Op(FieldScriptOpCode.IsEntityInRange)
                                           .Sources(Source.Immediate, Source.Persistent).Byte(OTHER).UShort(RESULT).Return());
        }

        private bool IsInRange(int entityId, int otherEntityId)
        {
            m_Asker = entityId;
            m_Other = otherEntityId;

            return m_InRange;
        }
    }
}
