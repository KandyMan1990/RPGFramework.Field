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
    internal sealed class FieldVariables : IRequiredVariables, IStartModule
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
        /// The volume the field plays its music at, 0 to 1, set by <c>SET_MUSIC_VOLUME</c> and <c>FADE_MUSIC_VOLUME</c>
        /// and kept across fields, battles and saves. Music started with <c>PLAY_MUSIC</c> plays at it.
        /// </summary>
        public const string MUSIC_VOLUME = "MusicVolume";

        private static readonly RequiredVariable[] s_Variables =
        {
            new RequiredVariable(CURRENT_FIELD,     MemoryBank.Persistent, VariableWidth.ULong, "The field the player is in. Its default is the field a new game begins in"),
            new RequiredVariable(CURRENT_SPAWN,     MemoryBank.Persistent, VariableWidth.Int,   "The spawn point the player entered the current field by. Its default is where a new game begins"),
            new RequiredVariable(PLAYER_POSITION_X, MemoryBank.Persistent, VariableWidth.Float, "Where the player stood when the game was saved. Its default is not used"),
            new RequiredVariable(PLAYER_POSITION_Y, MemoryBank.Persistent, VariableWidth.Float, "Where the player stood when the game was saved. Its default is not used"),
            new RequiredVariable(PLAYER_POSITION_Z, MemoryBank.Persistent, VariableWidth.Float, "Where the player stood when the game was saved. Its default is not used"),
            new RequiredVariable(PLAYER_FACING,     MemoryBank.Persistent, VariableWidth.Float, "Which way the player faced when the game was saved, in degrees around the field's up axis. Its default is not used"),
            new RequiredVariable(MUSIC_VOLUME,      MemoryBank.Persistent, VariableWidth.Float, "The volume the field plays its music at, 0 to 1, as SET_MUSIC_VOLUME and FADE_MUSIC_VOLUME leave it. Its default is a new game's", VariableDefaults.FromFloat(1f))
        };

        IReadOnlyList<RequiredVariable> IRequiredVariables.Variables => s_Variables;

        byte IStartModule.  ModuleId   => FieldConstants.MODULE_ID;
        string IStartModule.ModuleName => "Field";
    }
}
