using UnityEngine;

namespace RPGFramework.Field
{
    public sealed class FieldEntity : MonoBehaviour
    {
        public FieldScriptDefinition ScriptDefinition;
        public int                   EntityId => ScriptDefinition.EntityId;

        [Tooltip("Root entity always exists.  VisibleObject is the renderable representation controlled by the VISIBILITY op code.  Can be left empty")]
        [SerializeField]
        private GameObject m_VisibleObject;

        private void Awake()
        {
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            if (m_VisibleObject != null)
            {
                m_VisibleObject.SetActive(visible);
            }
        }
    }
}