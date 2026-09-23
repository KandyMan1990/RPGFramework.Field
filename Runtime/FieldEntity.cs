using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// An entity's body in the scene. Its id and scripts are its record in <see cref="FieldEntities" />, which the
    /// field module binds it to when the field loads.
    /// </summary>
    public sealed class FieldEntity : MonoBehaviour
    {
        /// <summary>
        /// No entity. Entity ids are authored from 0, so nothing ever has this one.
        /// </summary>
        internal const int NO_ENTITY = -1;

        [Tooltip("Root entity always exists.  VisibleObject is the renderable representation controlled by the VISIBILITY op code.  Can be left empty")]
        [SerializeField]
        private GameObject m_VisibleObject;

        private CompiledFieldEntity m_Record;

        internal int  EntityId         => m_Record != null ? m_Record.EntityId : NO_ENTITY;
        internal bool HasVisibleObject => m_VisibleObject != null;

        private void Awake()
        {
            SetVisible(false);
        }

        internal void Bind(CompiledFieldEntity record)
        {
            m_Record = record;
        }

        internal bool TryGetScriptIndex(FieldScriptType scriptType, out int eventId)
        {
            bool found = m_Record.TryGetScriptIndex(scriptType, out eventId);

            return found;
        }

        /// <summary>
        /// Authoring only: what <c>VISIBILITY</c> shows and hides.
        /// </summary>
        internal void SetVisibleObject(GameObject visibleObject)
        {
            m_VisibleObject = visibleObject;
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
