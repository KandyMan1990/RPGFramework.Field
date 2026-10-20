using NUnit.Framework;
using RPGFramework.Battle.SharedTypes;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    internal sealed class WhenTheBattleModeIsSet
    {
        private BattleFlags m_Mode;

        [SetUp]
        public void SetUp()
        {
            TestField field = new TestField();

            m_Mode = BattleFlags.DEFAULT;

            field.Vm.RequestSetBattleMode += OnMode;
            field.Run(new Script().Op(FieldScriptOpCode.SetBattleMode).Sources(Source.Immediate, Source.Immediate)
                                  .UShort((ushort)BattleFlags.DISABLE_GAME_OVER_ON_DEFEAT).Return());
        }

        [Test]
        public void ThenItAsksForItsFlags()
        {
            Assert.AreEqual(BattleFlags.DISABLE_GAME_OVER_ON_DEFEAT, m_Mode);
        }

        private void OnMode(BattleFlags mode)
        {
            m_Mode = mode;
        }
    }
}
