using RPGFramework.Battle.SharedTypes;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// What decides, as the player walks, whether a battle begins: the field's own picks from a table on the field, and a
    /// game binding its own in the field scene's installer replaces it. Battles walked into are scripts, and need none.
    /// </summary>
    public interface IFieldEncounterDriver
    {
        /// <summary>A field has been set up, new or returned to; what the driver reads of it is under this root.</summary>
        void Enter(GameObject field);

        /// <summary>
        /// The player has walked; true, with the battle, when one begins. Asked only of walking that counts: the
        /// player's own, not a script's, with random battles allowed, the field on show and clear of any fade, and
        /// nothing else asked for.
        /// </summary>
        bool TryStartBattle(in PlayerWalkArgs walk, out BattleArgs args);
    }
}
