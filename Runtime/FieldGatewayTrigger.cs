using System;
using UnityEngine;

namespace RPGFramework.Field
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class FieldGatewayTrigger : MonoBehaviour
    {
        public event Action<int, int> OnTriggered;

        private FieldEntity m_Entity;

        private void Awake()
        {
            m_Entity = GetComponentInParent<FieldEntity>();
        }

        private void OnTriggerEnter(Collider other)
        {
            FieldCompiledScript compiledScript = m_Entity.ScriptDefinition.GetScript(FieldScriptType.OnCollision);
            OnTriggered?.Invoke(m_Entity.EntityId, compiledScript.ScriptId);
        }
    }
}