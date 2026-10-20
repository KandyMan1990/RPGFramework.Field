using System;
using NUnit.Framework;
using RPGFramework.Core.Input;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    internal sealed class WhenAScriptWaitsForAButton
    {
        private const ushort AFTER = 200;

        private TestField          m_Field;
        private FieldEntityRuntime m_Entity;
        private ControlSlot        m_Listened;
        private bool               m_Pressed;

        [SetUp]
        public void SetUp()
        {
            m_Field    = new TestField();
            m_Listened = ControlSlot.Select;
            m_Pressed  = false;

            m_Field.Vm.StartWaitingForButton = Listen;

            m_Entity = m_Field.Start(new Script().Op(FieldScriptOpCode.WaitForButton).Sources(Source.Immediate, Source.Immediate)
                                                 .Byte((byte)ControlSlot.Primary).Count(AFTER, 40).Return());
            m_Field.Tick(m_Entity, 3);
        }

        [Test]
        public void ThenTheFieldListensForThatButton()
        {
            Assert.AreEqual(ControlSlot.Primary, m_Listened);
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

        private Func<bool> Listen(ControlSlot button)
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
