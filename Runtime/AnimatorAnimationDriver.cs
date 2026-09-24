using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// Drives an entity's animation through a Unity <see cref="Animator" />. One driver serves both dimensions: a
    /// sprite's clip and a model's clip are both Animator states, so only the game's controller differs.
    /// </summary>
    internal sealed class AnimatorAnimationDriver : MonoBehaviour, IAnimationDriver
    {
        /// <summary>
        /// How fast the entity is moving, in world units per second. A locomotion blend tree reads this; where
        /// standing becomes walking, and walking running, is the game's to decide and lives in its controller.
        /// </summary>
        private const string SPEED_PARAMETER = "Speed";

        /// <summary>
        /// Which way the entity faces, as a normalised direction. A 3D entity turns its body and has no use for
        /// these; a 2D one cannot turn a sprite, so it picks a facing from them instead.
        /// </summary>
        private const string FACING_X_PARAMETER = "FacingX";

        private const string FACING_Y_PARAMETER = "FacingY";

        private const float CROSS_FADE_SECONDS = 0.15f;

        private const float FACING_EPSILON = 0.0001f;

        private Animator m_Animator;
        private string   m_BaseStateName;
        private Vector3  m_Facing;
        private float    m_SpeedMultiplier;
        private bool     m_BaseStatePending;
        private bool     m_ParametersPending;

        private int m_SpeedParameter;
        private int m_FacingXParameter;
        private int m_FacingYParameter;

        private bool m_HasSpeedParameter;
        private bool m_HasFacingParameters;

        internal void Init(Animator animator)
        {
            m_Animator      = animator;
            m_BaseStateName = null;
            m_Facing        = Vector3.zero;

            m_SpeedMultiplier = 1f;

            m_BaseStatePending = false;

            m_SpeedParameter   = Animator.StringToHash(SPEED_PARAMETER);
            m_FacingXParameter = Animator.StringToHash(FACING_X_PARAMETER);
            m_FacingYParameter = Animator.StringToHash(FACING_Y_PARAMETER);

            m_ParametersPending = true;
        }

        void IAnimationDriver.SetBaseAnimation(string stateName)
        {
            m_BaseStateName    = stateName;
            m_BaseStatePending = true;

            ApplyBaseState();
        }

        void IAnimationDriver.SetSpeedMultiplier(float multiplier)
        {
            m_SpeedMultiplier = multiplier;
            m_Animator.speed  = multiplier;
        }

        void IAnimationDriver.SetPaused(bool paused)
        {
            m_Animator.speed = paused ? 0f : m_SpeedMultiplier;
        }

        void IAnimationDriver.ReturnToBase()
        {
            m_BaseStatePending = true;

            ApplyBaseState();
        }

        /// <summary>
        /// An entity's visuals start hidden and are shown by <c>VISIBILITY</c>. An Animator on a hidden object does
        /// not run, and loses a state asked for while it was hidden, so the base state is held until it can be given.
        /// </summary>
        private void ApplyBaseState()
        {
            if (m_BaseStateName == null || !m_Animator.isActiveAndEnabled)
            {
                return;
            }

            m_Animator.CrossFade(m_BaseStateName, CROSS_FADE_SECONDS);

            m_BaseStatePending = false;
        }

        void IAnimationDriver.Tick(Vector3 velocity)
        {
            if (!m_Animator.isActiveAndEnabled)
            {
                return;
            }

            if (m_ParametersPending)
            {
                DiscoverParameters();
            }

            if (m_BaseStatePending)
            {
                ApplyBaseState();
            }

            float speed = velocity.magnitude;

            if (m_HasSpeedParameter)
            {
                m_Animator.SetFloat(m_SpeedParameter, speed);
            }

            if (!m_HasFacingParameters)
            {
                return;
            }

            // A standing entity still faces somewhere, so the last direction it moved in is kept.
            if (speed > FACING_EPSILON)
            {
                m_Facing = velocity / speed;
            }

            m_Animator.SetFloat(m_FacingXParameter, m_Facing.x);
            m_Animator.SetFloat(m_FacingYParameter, m_Facing.y);
        }

        /// <summary>
        /// Which parameters a controller offers is the game's choice, not an error: a blend tree needs speed, a sprite
        /// needs facing, and a controller with one state per gait needs neither. It can only be asked once the
        /// Animator is running, though — reading them from a hidden one answers nothing and warns.
        /// </summary>
        private void DiscoverParameters()
        {
            m_HasSpeedParameter   = HasParameter(m_Animator, SPEED_PARAMETER);
            m_HasFacingParameters = HasParameter(m_Animator, FACING_X_PARAMETER) && HasParameter(m_Animator, FACING_Y_PARAMETER);

            m_ParametersPending = false;
        }

        private static bool HasParameter(Animator animator, string parameterName)
        {
            bool hasParameter = false;

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.name == parameterName)
                {
                    hasParameter = true;
                    break;
                }
            }

            return hasParameter;
        }
    }
}
