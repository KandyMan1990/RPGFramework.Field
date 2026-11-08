using NUnit.Framework;
using RPGFramework.Core.Memory;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Scheduling
{
    /// <summary>An entity whose script 0 runs in Main and calls its others.</summary>
    internal sealed class WhenAScriptCallsAnother
    {
        private const ushort TEMP    = 5;
        private const ushort COPIED  = 36;
        private const ushort INSIDE  = 40;
        private const ushort AFTER   = 44;
        private const ushort DEEPER  = 48;
        private const byte   URGENCY = 7;

        private TestField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();
        }

        [Test]
        public void ThenTheCalledScriptRuns()
        {
            Run(new Script().Call(1).Return(), new Script().Count(INSIDE, 1).ReturnFromCall());

            Assert.AreEqual(1, m_Field.CountAt(INSIDE));
        }

        [Test]
        public void ThenTheCallerCarriesOnFromTheNextLine()
        {
            Run(new Script().Call(1).Count(AFTER, 1).Return(), new Script().ReturnFromCall());

            Assert.AreEqual(1, m_Field.CountAt(AFTER));
        }

        [Test]
        public void ThenCallsNest()
        {
            Run(new Script().Call(1).Count(AFTER, 1).Return(),
                new Script().Call(2).Count(INSIDE, 1).ReturnFromCall(),
                new Script().Count(DEEPER, 1).ReturnFromCall());

            Assert.AreEqual(1, m_Field.CountAt(DEEPER));
            Assert.AreEqual(1, m_Field.CountAt(INSIDE));
            Assert.AreEqual(1, m_Field.CountAt(AFTER));
        }

        [Test]
        public void ThenAReturnInsideItEndsTheCallerToo()
        {
            FieldEntityRuntime entity = Run(new Script().Call(1).Count(AFTER, 1).Return(), new Script().Return());

            Assert.AreEqual(0, m_Field.CountAt(AFTER));
            Assert.IsFalse(entity.IsSlotOccupied(0));
        }

        [Test]
        public void ThenReturningFromACallOutsideOneEndsTheScript()
        {
            FieldEntityRuntime entity = Run(new Script().ReturnFromCall().Count(AFTER, 1).Return());

            Assert.AreEqual(0, m_Field.CountAt(AFTER));
            Assert.IsFalse(entity.IsSlotOccupied(0));
        }

        [Test]
        public void ThenItsTempIsItsOwn()
        {
            Run(WriteTemp(new Script(), 42).Call(1).Return(), CopyTemp(new Script()).ReturnFromCall());

            Assert.AreEqual(0, m_Field.Memory.ReadByte(MemoryBank.Persistent, COPIED));
        }

        [Test]
        public void ThenTheCallersTempIsAsItWas()
        {
            Run(CopyTemp(WriteTemp(new Script(), 42).Call(1)).Return(), WriteTemp(new Script(), 99).ReturnFromCall());

            Assert.AreEqual(42, m_Field.Memory.ReadByte(MemoryBank.Persistent, COPIED));
        }

        [Test]
        public void ThenTheSlotStillHoldsTheCaller()
        {
            int                caller = m_Field.Add(new Script().Call(1).Return());
            FieldEntityRuntime entity = m_Field.AddEntity(1, new byte[2], caller, m_Field.Add(new Script().Yield().ReturnFromCall()));

            m_Field.Tick(entity);

            Assert.IsTrue(entity.HoldsScript(caller, 0));
        }

        [Test]
        public void ThenWaitingInsideItWaitsForTheCaller()
        {
            FieldEntityRuntime entity = Run(new Script().Call(1).Count(AFTER, 1).Return(), new Script().Yield().ReturnFromCall());

            Assert.AreEqual(0, m_Field.CountAt(AFTER));

            m_Field.Tick(entity);

            Assert.AreEqual(1, m_Field.CountAt(AFTER));
        }

        [Test]
        public void ThenAnInterruptionPutsItBackInsideTheCall()
        {
            int                urgent = m_Field.Add(new Script().Return());
            FieldEntityRuntime entity = m_Field.AddEntity(1, new byte[] { 0, 0, URGENCY },
                                                          m_Field.Add(new Script().Call(1).Count(AFTER, 1).Return()),
                                                          m_Field.Add(new Script().Yield().Count(INSIDE, 1).ReturnFromCall()),
                                                          urgent);

            m_Field.Tick(entity);
            entity.TryRequestScript(urgent, URGENCY);
            m_Field.Tick(entity);

            Assert.AreEqual(1, m_Field.CountAt(INSIDE));
            Assert.AreEqual(1, m_Field.CountAt(AFTER));
        }

        private FieldEntityRuntime Run(params Script[] scripts)
        {
            int[] scriptIds = new int[scripts.Length];

            for (int i = 0; i < scripts.Length; i++)
            {
                scriptIds[i] = m_Field.Add(scripts[i]);
            }

            FieldEntityRuntime entity = m_Field.AddEntity(1, new byte[scripts.Length], scriptIds);

            m_Field.Tick(entity);

            return entity;
        }

        private static Script WriteTemp(Script script, byte value)
        {
            return script.Op(FieldScriptOpCode.SetByte).Sources(Source.Temp, Source.Immediate).UShort(TEMP).Byte(value);
        }

        private static Script CopyTemp(Script script)
        {
            return script.Op(FieldScriptOpCode.SetByte).Sources(Source.Persistent, Source.Temp).UShort(COPIED).Variable(VariableWidth.Byte, TEMP);
        }
    }
}
