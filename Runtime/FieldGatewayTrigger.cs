using System;
using UnityEngine;

namespace RPGFramework.Field
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class FieldGatewayTrigger : MonoBehaviour
    {
        private const string PLAYER_TAG = "Player";

        public event Action<int, int> OnTriggered;

        private FieldEntity m_Entity;
        private bool        m_IsActive;
        private bool        m_IsEntityShown;
        private int         m_EntityId;

        private void Awake()
        {
            m_Entity        = GetComponentInParent<FieldEntity>();
            m_IsActive      = true;
            m_IsEntityShown = true;
            m_EntityId      = m_Entity.EntityId;
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

            // TODO: don't want to rely on tag, need to know player entity ID
            if (!other.CompareTag(PLAYER_TAG))
            {
                return;
            }

            if (!m_Entity.ScriptDefinition.TryGetScriptIndex(FieldScriptType.OnCollision, out int eventId))
            {
                return;
            }

            OnTriggered?.Invoke(m_EntityId, eventId);
        }

        /// <summary>
        /// Turns every gateway in the field on or off, separately from whether this entity is shown.
        /// </summary>
        public void SetActive(bool active)
        {
            m_IsActive = active;
        }

        public void SetEntityShown(bool shown)
        {
            m_IsEntityShown = shown;
        }
    }
}