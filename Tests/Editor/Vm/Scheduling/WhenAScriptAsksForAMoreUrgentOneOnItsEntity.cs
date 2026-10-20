using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Scheduling
{
    internal sealed class WhenAScriptAsksForAMoreUrgentOneOnItsEntity
    {
        private const byte   ENTITY    = 1;
        private const ushort REQUESTER = 200;
        private const ushort REQUESTED = 204;

        private TestField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();

            int requester = m_Field.Add(new Script().Request(FieldScriptOpCode.RunAnotherEntityScriptUnlessBusy, ENTITY, 1).Count(REQUESTER, 40).Return());
            int requested = m_Field.Add(new Script().Count(REQUESTED, 40).Return());

            m_Field.Tick(m_Field.AddEntity(ENTITY, new byte[] { 0, 1 }, requester, requested));
        }

        [Test]
        public void ThenTheAskingScriptStopsThere()
        {
            Assert.AreEqual(0, m_Field.CountAt(REQUESTER));
        }

        [Test]
        public void ThenTheScriptAskedForRunsOnWhatIsLeftOfTheFrame()
        {
            Assert.AreEqual(FieldVM.INSTRUCTIONS_PER_ENTITY_PER_FRAME - 1, m_Field.CountAt(REQUESTED));
        }
    }
}
