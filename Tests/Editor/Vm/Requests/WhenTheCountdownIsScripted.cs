using NUnit.Framework;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    /// <summary>A script sets the countdown, shows it, reads what is left and hides it.</summary>
    internal sealed class WhenTheCountdownIsScripted
    {
        private const ushort LEFT = 40;

        private TestField m_Field;
        private int       m_Seconds;
        private int       m_X;
        private int       m_Y;
        private bool      m_Hidden;

        [SetUp]
        public void SetUp()
        {
            m_Field   = new TestField();
            m_Seconds = 0;
            m_X       = 0;
            m_Y       = 0;
            m_Hidden  = false;

            m_Field.Vm.RequestSetCountdown  += OnSetCountdown;
            m_Field.Vm.RequestShowCountdown += OnShowCountdown;
            m_Field.Vm.RequestHideCountdown += OnHideCountdown;
            m_Field.Vm.GetCountdown         =  SecondsLeft;

            m_Field.Run(new Script().Op(FieldScriptOpCode.SetCountdownTimer).Sources(Source.Immediate, Source.Immediate).Int(300)
                                    .Op(FieldScriptOpCode.ShowCountdownTimer).Sources(Source.Immediate, Source.Immediate).Int(20).Int(220)
                                    .Op(FieldScriptOpCode.GetCountdownTimer).Sources(Source.Persistent, Source.Immediate).UShort(LEFT)
                                    .Op(FieldScriptOpCode.HideCountdownTimer)
                                    .Return());
        }

        [Test]
        public void ThenItSetsTheSeconds()
        {
            Assert.AreEqual(300, m_Seconds);
        }

        [Test]
        public void ThenItShowsItWhereTheScriptSays()
        {
            Assert.AreEqual(20, m_X);
            Assert.AreEqual(220, m_Y);
        }

        [Test]
        public void ThenItStoresTheSecondsLeft()
        {
            Assert.AreEqual(299, m_Field.Memory.ReadInt(MemoryBank.Persistent, LEFT));
        }

        [Test]
        public void ThenItHidesIt()
        {
            Assert.IsTrue(m_Hidden);
        }

        private void OnSetCountdown(int seconds)
        {
            m_Seconds = seconds;
        }

        private void OnShowCountdown(int x, int y)
        {
            m_X = x;
            m_Y = y;
        }

        private void OnHideCountdown()
        {
            m_Hidden = true;
        }

        private static int SecondsLeft()
        {
            return 299;
        }
    }
}
