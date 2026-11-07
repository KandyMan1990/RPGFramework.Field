using System.Collections.Generic;

namespace RPGFramework.Field
{
    /// <summary>The opcodes of a game that adds none: the field scene binds it unless the game's installer binds its own.</summary>
    internal sealed class NullFieldGameOpCodeDatabase : IFieldGameOpCodeDatabase
    {
        private readonly Dictionary<ushort, FieldOpCodeHandler> m_Handlers;

        internal NullFieldGameOpCodeDatabase()
        {
            m_Handlers = new Dictionary<ushort, FieldOpCodeHandler>();
        }

        IReadOnlyDictionary<ushort, FieldOpCodeHandler> IFieldGameOpCodeDatabase.Handlers => m_Handlers;
    }
}
