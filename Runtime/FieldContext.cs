using System.Collections.Generic;
using UnityEngine;

namespace RPGFramework.Field
{
    internal sealed class FieldContext
    {
        internal IReadOnlyList<FieldEntityRuntime>       Entities             => m_Entities;
        internal FieldVM                                 VM                   { get; }
        internal FieldEntityRuntime                      PlayerEntity         => m_PlayerEntity;
        internal IReadOnlyList<int>                      VisibleEntityIds     => m_VisibleEntityIds;
        internal IReadOnlyDictionary<int, Vector3>       EntityPositions      => m_EntityPositions;
        internal IReadOnlyDictionary<int, Quaternion>    EntityRotations      => m_EntityRotations;
        internal IReadOnlyDictionary<int, RotationState> EntityRotationStates => m_EntityRotationStates;

        private readonly List<FieldEntityRuntime>       m_Entities;
        private readonly List<int>                      m_VisibleEntityIds;
        private readonly Dictionary<int, Vector3>       m_EntityPositions;
        private readonly Dictionary<int, Quaternion>    m_EntityRotations;
        private readonly Dictionary<int, RotationState> m_EntityRotationStates;
        private          FieldEntityRuntime             m_PlayerEntity;

        internal FieldContext(FieldVM vm, List<FieldEntityRuntime> entities)
        {
            VM                     = vm;
            m_Entities             = entities;
            m_VisibleEntityIds     = new List<int>();
            m_EntityPositions      = new Dictionary<int, Vector3>(entities.Count);
            m_EntityRotations      = new Dictionary<int, Quaternion>(entities.Count);
            m_EntityRotationStates = new Dictionary<int, RotationState>();
            m_EntityRotationStates = new Dictionary<int, RotationState>();
        }

        internal void SetPlayerEntity(FieldEntityRuntime playerEntity) => m_PlayerEntity = playerEntity;

        internal void SetEntityVisible(int entityId, bool visible)
        {
            if (visible && !m_VisibleEntityIds.Contains(entityId))
            {
                m_VisibleEntityIds.Add(entityId);
            }
            else if (!visible && m_VisibleEntityIds.Contains(entityId))
            {
                m_VisibleEntityIds.Remove(entityId);
            }
        }

        internal void SetEntityPositionAndRotation(int entityId, Transform transform)
        {
            m_EntityPositions[entityId] = transform.position;
            m_EntityRotations[entityId] = transform.rotation;
        }

        internal void SetEntityRotationState(int entityId, RotationState rotationState)
        {
            m_EntityRotationStates[entityId] = rotationState;
        }
    }
}