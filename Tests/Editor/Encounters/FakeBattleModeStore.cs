using RPGFramework.Battle.SharedTypes;
using RPGFramework.Battle.SharedTypes.Stores;

namespace RPGFramework.Field.Tests.Encounters
{
    internal sealed class FakeBattleModeStore : IBattleModeStore
    {
        private BattleFlags m_Mode;

        internal FakeBattleModeStore(BattleFlags mode)
        {
            m_Mode = mode;
        }

        BattleFlags IBattleModeStore.Mode => m_Mode;

        void IBattleModeStore.SetMode(BattleFlags mode)
        {
            m_Mode = mode;
        }
    }
}
