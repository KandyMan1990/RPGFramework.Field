using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Scheduling
{
    internal sealed class WhenAnEntityHasNoDefaultScript
    {
        private FieldEntityRuntime m_Entity;

        [SetUp]
        public void SetUp()
        {
            TestField field = new TestField();

            m_Entity = field.AddEntity(1, new byte[1], field.Add(new Script().Return()));

            m_Entity.RunInitScript(field.Vm);
            field.Vm.StartDefaultScript(1, -1);
        }

        [Test]
        public void ThenMainIsLeftEmpty()
        {
            Assert.IsFalse(m_Entity.IsSlotOccupied((byte)FieldScriptPriority.Main));
        }
    }
}
