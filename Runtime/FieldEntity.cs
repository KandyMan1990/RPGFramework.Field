using UnityEngine;

namespace RPGFramework.Field
{
    public sealed class FieldEntity : MonoBehaviour
    {
        public FieldScriptDefinition ScriptDefinition;

        [Tooltip("Root entity always exists.  VisibleObject is the renderable representation controlled by the VISIBILITY op code.  Can be left empty")]
        [SerializeField]
        private GameObject m_VisibleObject;

        public int EntityId => ScriptDefinition.EntityId;

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