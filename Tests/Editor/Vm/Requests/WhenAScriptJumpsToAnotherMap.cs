using NUnit.Framework;
using RPGFramework.Core.SharedTypes;
using RPGFramework.Field.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    internal sealed class WhenAScriptJumpsToAnotherMap
    {
        private const ulong  MAP   = 0x1122334455667788UL;
        private const ushort AFTER = 200;

        private TestField m_Field;
        private FieldArgs m_Args;
        private bool      m_Asked;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();
            m_Args  = default;
            m_Asked = false;

            m_Field.Vm.RequestFieldTransition += OnTransition;

            FieldEntityRuntime entity = m_Field.Start(new Script().Op(FieldScriptOpCode.JumpToAnotherMap).ULong(MAP)
                                                                  .Sources(Source.Immediate, Source.Immediate).Int(3)
                                                                  .Count(AFTER, 40).Return());

            m_Field.Tick(entity, 3);
        }

        [Test]
        public void ThenItAsksForTheMap()
        {
            Assert.IsTrue(m_Asked);
        }

        [Test]
        public void ThenItAsksForTheMapItNames()
        {
            Assert.AreEqual(MAP, m_Args.FieldId);
        }

        [Test]
        public void ThenItAsksForTheSpawnPointItNames()
        {
            Assert.AreEqual(3, m_Args.SpawnId);
        }

        [Test]
        public void ThenNothingAfterItRuns()
        {
            Assert.AreEqual(0, m_Field.CountAt(AFTER));
        }

        private void OnTransition(FieldArgs args)
        {
            m_Args  = args;
            m_Asked = true;
        }
    }
}
