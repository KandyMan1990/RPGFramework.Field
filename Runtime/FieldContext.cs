using System.Collections.Generic;

namespace RPGFramework.Field
{
    internal sealed class FieldContext
    {
        internal IReadOnlyList<FieldEntityRuntime> Entities => m_Entities;
        internal FieldVM                           VM       { get; }

        private readonly List<FieldEntityRuntime> m_Entities;

        internal FieldContext(FieldVM vm, List<FieldEntityRuntime> entities)
        {
            VM         = vm;
            m_Entities = entities;
        }
    }
}