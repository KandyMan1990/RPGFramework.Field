using System;
using UnityEngine;

namespace RPGFramework.Field
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class FieldGatewayTrigger : MonoBehaviour
    {
        public event Action<int, int> OnTriggered;

        private FieldEntity m_Entity;
        private bool        m_IsActive;
        private bool        m_IsEntityShown;
        private int         m_EntityId;
        private int         m_PlayerEntityId;

        private void Awake()
        {
            m_Entity         = GetComponentInParent<FieldEntity>();
            m_IsActive       = true;
            m_IsEntityShown  = true;
            m_EntityId       = m_Entity.EntityId;
            m_PlayerEntityId = FieldEntity.NO_ENTITY;
        }

        private void OnTriggerEnter(Collider other)
        {
            TriggerLogic(other);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TriggerLogic(other);
        }

        private void TriggerLogic(Component other)
        {
            if (!m_IsActive || !m_IsEntityShown)
            {
                return;
            }

            FieldEntity entity = other.GetComponentInParent<FieldEntity>();

            if (entity == null || entity.EntityId != m_PlayerEntityId)
            {
                return;
            }

            if (!m_Entity.ScriptDefinition.TryGetScriptIndex(FieldScriptType.OnCollision, out int eventId))
            {
                return;
            }

            OnTriggered?.Invoke(m_EntityId, eventId);
        }

        public void SetActive(bool active)
        {
            m_IsActive = active;
        }

        public void SetEntityShown(bool shown)
        {
            m_IsEntityShown = shown;
        }

        public void SetPlayerEntityId(int entityId)
        {
            m_PlayerEntityId = entityId;
        }
    }
}