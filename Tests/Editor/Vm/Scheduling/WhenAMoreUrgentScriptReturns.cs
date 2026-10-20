using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Scheduling
{
    internal sealed class WhenAMoreUrgentScriptReturns
    {
        private const ushort BELOW = 200;

        private TestField m_Field;

        [SetUp]
        public void SetUp()
        {
            m_Field = new TestField();

            FieldEntityRuntime entity = m_Field.Start(new Script().Count(BELOW, 40).Return());

            entity.TryRequestScript(m_Field.Add(new Script().Return()), 1);
            m_Field.Tick(entity);
        }

        [Test]
        public void ThenTheScriptBelowRunsOnWhatIsLeftOfTheFrame()
        {
            Assert.AreEqual(FieldVM.INSTRUCTIONS_PER_ENTITY_PER_FRAME - 1, m_Field.CountAt(BELOW));
        }
    }
}
