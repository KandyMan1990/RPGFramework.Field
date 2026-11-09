using System.Collections.Generic;
using Unity.Mathematics;
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

        [Tooltip("The bone a script turns to make this entity look at something. A humanoid's head is found for you, so leave this empty; on a generic rig, pick its head bone")]
        [SerializeField]
        private Transform m_Head;

        [Tooltip("Where in the walk animation's loop one foot touches the ground, and a footstep sounds: 0 is the start of the loop and 1 the end. In a one-second walk with a foot landing 0.3 seconds in, this is 0.3. Move it if steps sound early or late")]
        [SerializeField]
        [Range(0f, 1f)]
        private float m_FirstFootDown;

        [Tooltip("Where in the walk animation's loop the other foot touches the ground, 0 the start of the loop and 1 the end")]
        [SerializeField]
        [Range(0f, 1f)]
        private float m_SecondFootDown = 0.5f;

        [Tooltip("Leave off unless a component of your own beside the Animator receives the events on its clips. With none to receive them Unity logs an error for each one")]
        [SerializeField]
        private bool m_RaisesAnimationEvents;

        private CompiledFieldEntity m_Record;

        internal int EntityId => m_Record != null ? m_Record.EntityId : NO_ENTITY;

        private bool IsSolid { get; set; } = true;
        private bool IsShown { get; set; } = true;

        private bool m_PushActive = true;

        private Vector3 m_VisibleRestPosition;

        private Quaternion m_HeadAnimated;
        private Quaternion m_HeadLeftAt;

        internal bool RaisesPush => m_PushActive && IsShown;

        /// <summary>Where a head looks to find this entity: its own head, or where it stands if it has none.</summary>
        internal Vector3 LookPoint => m_Head != null ? m_Head.position : transform.position;

        private void Awake()
        {
            if (m_VisibleObject != null)
            {
                m_VisibleRestPosition = m_VisibleObject.transform.localPosition;
            }

            Animator animator = GetComponentInChildren<Animator>(true);

            if (animator != null)
            {
                animator.fireEvents = m_RaisesAnimationEvents;

                if (m_Head == null && animator.isHuman)
                {
                    m_Head = animator.GetBoneTransform(HumanBodyBones.Head);
                }
            }

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

#if UNITY_EDITOR
        internal bool HasVisibleObject => m_VisibleObject != null;

        /// <summary>Authoring only: whether a script can turn this body's head, read before anything has run.</summary>
        internal bool HasHead
        {
            get
            {
                if (m_Head != null)
                {
                    return true;
                }

                Animator animator = GetComponentInChildren<Animator>(true);

                return animator != null && animator.avatar != null && animator.avatar.isHuman;
            }
        }

        /// <summary>
        /// Authoring only: what <c>VISIBILITY</c> shows and hides.
        /// </summary>
        internal void SetVisibleObject(GameObject visibleObject)
        {
            m_VisibleObject = visibleObject;
        }
#endif

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

        /// <summary>Whether a foot has landed as a cycle went forward from one point to another.</summary>
        internal bool FootLandedBetween(float previous, float current)
        {
            return Footsteps.Crossed(previous, current, m_FirstFootDown) || Footsteps.Crossed(previous, current, m_SecondFootDown);
        }

        /// <summary>
        /// How far the visible object sits from where it was placed, along the world's axes whichever way the body faces.
        /// </summary>
        internal void SetDrawOffset(Vector3 offset)
        {
            if (m_VisibleObject == null)
            {
                return;
            }

            Transform visible = m_VisibleObject.transform;

            visible.localPosition = m_VisibleRestPosition + visible.parent.InverseTransformVector(offset);
        }

        /// <summary>
        /// Turns the head from where its animation put it, about the body's own axes so a rig need not say which way its
        /// bone points. An Animator that has not written the bone since leaves last frame's turn on it, so that is
        /// taken off rather than added to.
        /// </summary>
        internal void TurnHead(float2 angles)
        {
            if (m_Head.localRotation != m_HeadLeftAt)
            {
                m_HeadAnimated = m_Head.localRotation;
            }

            Quaternion body     = transform.rotation;
            Quaternion animated = m_Head.parent.rotation * m_HeadAnimated;

            m_Head.rotation = body * Quaternion.Euler(-angles.y, angles.x, 0f) * Quaternion.Inverse(body) * animated;
            m_HeadLeftAt    = m_Head.localRotation;
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