using UnityEngine;

namespace RPGFramework.Field
{
    public sealed class FieldEntity : MonoBehaviour
    {
        /// <summary>
        /// No entity. Entity ids are authored from 0, so nothing ever has this one.
        /// </summary>
        internal const int NO_ENTITY = -1;

        public FieldScriptDefinition ScriptDefinition;
        public int                   EntityId => ScriptDefinition.EntityId;

        [Tooltip("Root entity always exists.  VisibleObject is the renderable representation controlled by the VISIBILITY op code.  Can be left empty")]
        [SerializeField]
        private GameObject m_VisibleObject;

        private void Awake()
        {
            SetVisible(false);
        }

        internal void SetVisible(bool visible)
        {
            if (m_VisibleObject != null)
            {
                m_VisibleObject.SetActive(visible);
            }
        }
    }
}