using System.Collections.Generic;
using RPGFramework.Core.Memory;
using RPGFramework.Core.SharedTypes;
using RPGFramework.Field.SharedTypes.Constants;

namespace RPGFramework.Field
{
    /// <summary>
    /// The variables the field module requires of every game's map, and the field module's place among the
    /// modules a new game can begin in.
    /// </summary>
    internal sealed class FieldVariables
#if UNITY_EDITOR
        : IRequiredVariables, IStartModule
#endif
    {
        /// <summary>
        /// The field the player is in, as its name's hash. Its default is the field a new game begins in.
        /// </summary>
        public const string CURRENT_FIELD = "CurrentField";

        /// <summary>
        /// The spawn point the player entered the current field by. Its default is where a new game begins.
        /// </summary>
        public const string CURRENT_SPAWN = "CurrentSpawn";

        /// <summary>
        /// Where the player stood when the field last handed over to the menu, so a save made there holds it.
        /// </summary>
        public const string PLAYER_POSITION_X = "PlayerPositionX";

        public const string PLAYER_POSITION_Y = "PlayerPositionY";

        public const string PLAYER_POSITION_Z = "PlayerPositionZ";

        /// <summary>
        /// Which way the player faced when the field last handed over to the menu, in degrees around the field's up
        /// axis.
        /// </summary>
        public const string PLAYER_FACING = "PlayerFacing";

        /// <summary>
        /// The field a game over sends the player to, as its name's hash. Only its default is used: the game's choice.
        /// </summary>
        public const string GAME_OVER_FIELD = "GameOverField";

        /// <summary>
        /// The spawn point the player arrives at in the game-over field. Only its default is used: the game's choice.
        /// </summary>
        public const string GAME_OVER_SPAWN = "GameOverSpawn";

        /// <summary>
        /// Where a script last left the field's screen beneath its windows: clear, faded out, or a colour taken from it
        /// or added (<see cref="ScreenFade" />), so a field left faded or tinted is still so after a battle or a load.
        /// </summary>
        public const string SCREEN_FADE = "ScreenFade";

        /// <summary>The transition a faded-out screen is covered by, as its name's hash; 0 for the default cover.</summary>
        public const string SCREEN_FADE_TRANSITION = "ScreenFadeTransition";

        /// <summary>The colour taken from or added to the screen, red, green and blue a byte each, red highest.</summary>
        public const string SCREEN_COLOUR = "ScreenColour";

#if UNITY_EDITOR
        private static readonly RequiredVariable[] m_Variables =
        {
            new RequiredVariable(CURRENT_FIELD,          MemoryBank.Persistent, VariableWidth.ULong, "The field the player is in. Its default is the field a new game begins in"),
            new RequiredVariable(CURRENT_SPAWN,          MemoryBank.Persistent, VariableWidth.Int,   "The spawn point the player entered the current field by. Its default is where a new game begins"),
            new RequiredVariable(PLAYER_POSITION_X,      MemoryBank.Persistent, VariableWidth.Float, "Where the player stood when the game was saved. Its default is not used"),
            new RequiredVariable(PLAYER_POSITION_Y,      MemoryBank.Persistent, VariableWidth.Float, "Where the player stood when the game was saved. Its default is not used"),
            new RequiredVariable(PLAYER_POSITION_Z,      MemoryBank.Persistent, VariableWidth.Float, "Where the player stood when the game was saved. Its default is not used"),
            new RequiredVariable(PLAYER_FACING,          MemoryBank.Persistent, VariableWidth.Float, "Which way the player faced when the game was saved, in degrees around the field's up axis. Its default is not used"),
            new RequiredVariable(GAME_OVER_FIELD,        MemoryBank.Session,    VariableWidth.ULong, "The field a game over sends the player to. Only its default is used"),
            new RequiredVariable(GAME_OVER_SPAWN,        MemoryBank.Session,    VariableWidth.Int,   "The spawn point the player arrives at in the game-over field. Only its default is used"),
            new RequiredVariable(SCREEN_FADE,            MemoryBank.Persistent, VariableWidth.Byte,  "Where a script left the field's screen: clear, faded out, or a colour taken from it or added. Its default, 0, is clear"),
            new RequiredVariable(SCREEN_FADE_TRANSITION, MemoryBank.Persistent, VariableWidth.ULong, "The transition a faded-out screen is covered by. Its default is not used"),
            new RequiredVariable(SCREEN_COLOUR,          MemoryBank.Persistent, VariableWidth.Int,   "The colour taken from or added to the field's screen. Its default is not used")
        };

        IReadOnlyList<RequiredVariable> IRequiredVariables.Variables => m_Variables;

        byte IStartModule.  ModuleId   => FieldConstants.MODULE_ID;
        string IStartModule.ModuleName => "Field";
#endif
    }
}
