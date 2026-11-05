using System;
using NUnit.Framework;
using RPGFramework.Hashing;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    internal sealed class WhenAScriptWaitsForAButton
    {
        private const ushort AFTER = 200;

        private TestField          m_Field;
        private FieldEntityRuntime m_Entity;
        private ulong              m_Listened;
        private bool               m_Pressed;

        [SetUp]
        public void SetUp()
        {
            m_Field    = new TestField();
            m_Listened = 0;
            m_Pressed  = false;

            m_Field.Vm.StartWaitingForButton = Listen;

            m_Entity = m_Field.Start(new Script().Op(FieldScriptOpCode.WaitForButton).ULong(Fnv1a64.Hash("Card"))
                                                 .Count(AFTER, 40).Return());
            m_Field.Tick(m_Entity, 3);
        }

        [Test]
        public void ThenTheFieldListensForThatButton()
        {
            Assert.AreEqual(Fnv1a64.Hash("Card"), m_Listened);
        }

        [Test]
        public void ThenItWaitsUntilItIsPressed()
        {
            Assert.AreEqual(0, m_Field.CountAt(AFTER));
        }

        [Test]
        public void ThenItCarriesOnOnceItIsPressed()
        {
            m_Pressed = true;
            m_Field.Tick(m_Entity, 2);

            Assert.Greater(m_Field.CountAt(AFTER), 0);
        }

        private Func<bool> Listen(ulong button)
        {
            m_Listened = button;

            return IsPressed;
        }

        private bool IsPressed()
        {
            return m_Pressed;
        }
    }
}
