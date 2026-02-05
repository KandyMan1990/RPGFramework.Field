using System.Collections.Generic;

namespace RPGFramework.Field
{
    internal sealed class FieldContext
    {
        internal IReadOnlyList<FieldEntityRuntime> Entities     => m_Entities;
        internal FieldVM                           VM           { get; }
        internal FieldEntityRuntime                PlayerEntity => m_PlayerEntity;

        private readonly List<FieldEntityRuntime> m_Entities;
        private          FieldEntityRuntime       m_PlayerEntity;

        internal FieldContext(FieldVM vm, List<FieldEntityRuntime> entities)
        {
            VM         = vm;
            m_Entities = entities;
        }

        internal void SetPlayerEntity(FieldEntityRuntime playerEntity) => m_PlayerEntity = playerEntity;
    }
}