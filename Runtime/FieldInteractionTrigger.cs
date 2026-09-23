using System;
using UnityEngine;

namespace RPGFramework.Field
{
    public sealed class FieldInteractionTrigger : MonoBehaviour
    {
        internal event Action<int, int> OnInteracted;

        internal float       InteractionAngle => m_InteractionAngle;
        internal float       InteractionRange => m_InteractionRange;
        internal bool        IsActive         => m_IsActive;
        internal FieldEntity Entity           => m_Entity;

        [SerializeField]
        [Range(0f, 360f)]
        private float m_InteractionAngle = 360f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("How far from the entity the player can be to interact with it")]
        private float m_InteractionRange = 1f;

        private FieldEntity m_Entity;
        private bool        m_IsActive;

        private void Awake()
        {
            m_Entity   = GetComponentInParent<FieldEntity>();
            m_IsActive = true;
        }

        internal void TryInteract()
        {
            if (!m_IsActive)
            {
                return;
            }

            if (!m_Entity.TryGetScriptIndex(FieldScriptType.OnInteraction, out int eventId))
            {
                return;
            }

            OnInteracted?.Invoke(m_Entity.EntityId, eventId);
        }

        internal void SetActive(bool active)
        {
            m_IsActive = active;
        }

        public void SetInteractionRange(float range)
        {
            m_InteractionRange = range;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            FieldEntity entity          = m_Entity != null ? m_Entity : GetComponentInParent<FieldEntity>();
            Transform   entityTransform = entity   != null ? entity.transform : transform;
            Vector3     position        = entityTransform.position;
            Vector3     arcStart        = Quaternion.Euler(0, -m_InteractionAngle / 2, 0) * entityTransform.forward;

            UnityEditor.Handles.color = new Color(0, 1, 0, 0.25f);
            UnityEditor.Handles.DrawSolidArc(position, Vector3.up, arcStart, m_InteractionAngle, m_InteractionRange);

            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.DrawWireArc(position, Vector3.up, arcStart, m_InteractionAngle, m_InteractionRange);
            UnityEditor.Handles.DrawWireDisc(position, Vector3.up, m_InteractionRange);
        }
#endif
    }
}