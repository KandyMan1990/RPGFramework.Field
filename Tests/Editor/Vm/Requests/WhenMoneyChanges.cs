using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    internal sealed class WhenMoneyChanges
    {
        private TestField m_Field;
        private int       m_Amount;

        [SetUp]
        public void SetUp()
        {
            m_Field  = new TestField();
            m_Amount = 0;

            m_Field.Vm.RequestAddMoney += amount => m_Amount = amount;
        }

        [Test]
        public void ThenAddingAsksForTheAmount()
        {
            Run(FieldScriptOpCode.AddMoney);

            Assert.AreEqual(500, m_Amount);
        }

        [Test]
        public void ThenSubtractingAsksForItTakenAway()
        {
            Run(FieldScriptOpCode.SubtractMoney);

            Assert.AreEqual(-500, m_Amount);
        }

        private void Run(FieldScriptOpCode opCode)
        {
            m_Field.Run(new Script().Op(opCode).Sources(Source.Immediate, Source.Immediate).Int(500).Return());
        }
    }
}
