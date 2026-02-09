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

        private void Awake()
        {
            m_Entity   = GetComponentInParent<FieldEntity>();
            m_IsActive = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!m_IsActive)
            {
                return;
            }

            FieldCompiledScript compiledScript = m_Entity.ScriptDefinition.GetScript(FieldScriptType.OnCollision);
            OnTriggered?.Invoke(m_Entity.EntityId, compiledScript.ScriptId);
        }

        public void SetActive(bool active)
        {
            m_IsActive = active;
        }
    }
}