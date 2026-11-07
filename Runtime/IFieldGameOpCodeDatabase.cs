using System.Collections.Generic;

namespace RPGFramework.Field
{
    /// <summary>
    /// A game's own field opcodes, each value, <c>0x8000</c> or above, to the method that runs it. A game implements it and
    /// binds it in the field scene's installer; the field scene binds one holding none unless the game has. The VM puts
    /// them in one table with the framework's, indexed by the opcode.
    /// </summary>
    public interface IFieldGameOpCodeDatabase
    {
        IReadOnlyDictionary<ushort, FieldOpCodeHandler> Handlers { get; }
    }
}
