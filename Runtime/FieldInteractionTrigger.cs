using System;
using UnityEngine;

namespace RPGFramework.Field
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class FieldInteractionTrigger : MonoBehaviour
    {
        public event Action<int, int> OnInteracted;
        public event Action<int>      OnTriggerEntered;
        public event Action<int>      OnTriggerExited;

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
            TriggerEnterLogic();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TriggerEnterLogic();
        }

        private void OnTriggerExit(Collider other)
        {
            TriggerExitLogic();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            TriggerExitLogic();
        }

        private void TriggerEnterLogic()
        {
            if (!m_IsActive)
            {
                return;
            }

            // TODO: if not player, return

            OnTriggerEntered?.Invoke(m_EntityId);
        }

        private void TriggerExitLogic()
        {
            if (!m_IsActive)
            {
                return;
            }

            OnTriggerExited?.Invoke(m_EntityId);
        }

        public void TryInteract()
        {
            if (!m_IsActive)
            {
                return;
            }
            
            // TODO: this probably needs to take in some arguments, probably the player entity, to check if it can interact or not

            FieldCompiledScript compiledScript = m_Entity.ScriptDefinition.GetScript(FieldScriptType.OnInteraction);

            OnInteracted?.Invoke(m_EntityId, compiledScript.ScriptId);
        }

        public void SetActive(bool active)
        {
            m_IsActive = active;
        }
    }
}