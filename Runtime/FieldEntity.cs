using System.Collections.Generic;
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

        private bool IsSolid { get; set; } = true;
        private bool IsShown { get; set; } = true;

        private bool m_PushActive = true;

        internal bool RaisesPush => m_PushActive && IsShown;

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

        /// <summary>
        /// Whether other entities are stopped by this one. Its own movement is unaffected, as the reference's through
        /// flag is: it is read only when something else moves into it.
        /// </summary>
        internal void SetSolid(bool solid)
        {
            IsSolid = solid;
        }

        internal void SetPushActive(bool active)
        {
            m_PushActive = active;
        }

        /// <summary>
        /// A hidden entity can be walked through and raises no push, as the reference's <c>HIDE</c> makes it.
        /// </summary>
        internal void SetShown(bool shown)
        {
            IsShown = shown;

            SetVisible(shown);
        }

        /// <summary>
        /// Whether a collider stops a moving entity: anything that is not an entity does, and an entity does unless a
        /// script has let others walk through it.
        /// </summary>
        internal static bool Blocks(Component collider)
        {
            FieldEntity entity = collider.GetComponentInParent<FieldEntity>();

            bool blocks = entity == null || entity.IsSolid && entity.IsShown;

            return blocks;
        }

        /// <summary>
        /// Note the entity a collider belongs to, once — how a driver reports what the player walked into.
        /// </summary>
        internal static void AddOwnerOf(Component collider, List<FieldEntity> entities)
        {
            FieldEntity entity = collider.GetComponentInParent<FieldEntity>();

            if (entity != null && !entities.Contains(entity))
            {
                entities.Add(entity);
            }
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