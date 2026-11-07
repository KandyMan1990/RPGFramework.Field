using System.Collections.Generic;

namespace RPGFramework.Field.Tests.GameOpCodes
{
    /// <summary>A game's opcodes as a test adds them, each value with its handler.</summary>
    internal sealed class TestOpCodeDatabase : IFieldGameOpCodeDatabase
    {
        private readonly Dictionary<ushort, FieldOpCodeHandler> m_Handlers;

        internal TestOpCodeDatabase()
        {
            m_Handlers = new Dictionary<ushort, FieldOpCodeHandler>();
        }

        IReadOnlyDictionary<ushort, FieldOpCodeHandler> IFieldGameOpCodeDatabase.Handlers => m_Handlers;

        internal TestOpCodeDatabase With(TestGameOpCode opCode, FieldOpCodeHandler handler)
        {
            m_Handlers.Add((ushort)opCode, handler);

            return this;
        }
    }
}
