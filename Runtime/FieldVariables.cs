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
    public sealed class FieldVariables : IRequiredVariables, IStartModule
    {
        /// <summary>
        /// The field the player is in, as its name's hash. Its default is the field a new game begins in.
        /// </summary>
        public const string CURRENT_FIELD = "CurrentField";

        /// <summary>
        /// The spawn point the player entered the current field by. Its default is where a new game begins.
        /// </summary>
        public const string CURRENT_SPAWN = "CurrentSpawn";

        private static readonly RequiredVariable[] s_Variables =
        {
            new RequiredVariable(CURRENT_FIELD, MemoryBank.Persistent, VariableWidth.ULong, "The field the player is in. Its default is the field a new game begins in"),
            new RequiredVariable(CURRENT_SPAWN, MemoryBank.Persistent, VariableWidth.Int,   "The spawn point the player entered the current field by. Its default is where a new game begins")
        };

        public IReadOnlyList<RequiredVariable> Variables => s_Variables;

        public byte   ModuleId   => FieldConstants.MODULE_ID;
        public string ModuleName => "Field";
    }
}
