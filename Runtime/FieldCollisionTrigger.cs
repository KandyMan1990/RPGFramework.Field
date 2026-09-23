using System;
using UnityEngine;

namespace RPGFramework.Field
{
    public sealed class FieldCollisionTrigger : MonoBehaviour
    {
        internal event Action<int, int> OnEntered;
        internal event Action<int, int> OnGatewayEntered;
        internal event Action<int, int> OnLeft;

        private FieldEntity m_Entity;
        private bool        m_IsActive;
        private bool        m_IsEntityShown;
        private int         m_PlayerEntityId;

        private void Awake()
        {
            m_Entity         = GetComponentInParent<FieldEntity>();
            m_IsActive       = true;
            m_IsEntityShown  = true;
            m_PlayerEntityId = FieldEntity.NO_ENTITY;
        }

        private void OnTriggerEnter(Collider other)
        {
            Enter(other);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Enter(other);
        }

        private void OnTriggerExit(Collider other)
        {
            Leave(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            Leave(other);
        }

        private void Enter(Component other)
        {
            if (TryGetPlayerScript(other, FieldScriptType.OnEnter, out int eventId))
            {
                OnEntered?.Invoke(m_Entity.EntityId, eventId);
            }

            if (TryGetPlayerScript(other, FieldScriptType.Gateway, out int gatewayEventId))
            {
                OnGatewayEntered?.Invoke(m_Entity.EntityId, gatewayEventId);
            }
        }

        private void Leave(Component other)
        {
            if (TryGetPlayerScript(other, FieldScriptType.OnLeave, out int eventId))
            {
                OnLeft?.Invoke(m_Entity.EntityId, eventId);
            }
        }

        private bool TryGetPlayerScript(Component other, FieldScriptType scriptType, out int eventId)
        {
            eventId = -1;

            if (!m_IsActive || !m_IsEntityShown)
            {
                return false;
            }

            FieldEntity entity = other.GetComponentInParent<FieldEntity>();

            if (entity == null || entity.EntityId != m_PlayerEntityId)
            {
                return false;
            }

            bool found = m_Entity.TryGetScriptIndex(scriptType, out eventId);

            return found;
        }

        internal void SetActive(bool active)
        {
            m_IsActive = active;
        }

        internal void SetEntityShown(bool shown)
        {
            m_IsEntityShown = shown;
        }

        internal void SetPlayerEntityId(int entityId)
        {
            m_PlayerEntityId = entityId;
        }
    }
}