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
        private int         m_EntityId;

        private void Awake()
        {
            m_Entity   = GetComponentInParent<FieldEntity>();
            m_IsActive = true;
            m_EntityId = m_Entity.EntityId;
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
            if (!m_IsActive)
            {
                return;
            }

            // TODO: don't want to rely on tag, need to know player entity ID
            if (!other.CompareTag(PLAYER_TAG))
            {
                return;
            }

            FieldCompiledScript compiledScript = m_Entity.ScriptDefinition.GetScript(FieldScriptType.OnCollision);
            OnTriggered?.Invoke(m_EntityId, compiledScript.ScriptId);
        }

        public void SetActive(bool active)
        {
            m_IsActive = active;
        }
    }
}