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

        /// <summary>Animation is driven on the base layer; the layers above it are the game's to use.</summary>
        private const int BASE_LAYER = 0;

        private Animator m_Animator;
        private string   m_BaseStateName;
        private bool     m_BaseStatePending;
        private bool     m_ParametersPending;
        private Vector3  m_Facing;
        private float    m_SpeedMultiplier;
        private bool     m_Paused;

        private int               m_PlayingState;
        private AnimationPlayMode m_PlayMode;
        private float             m_PlayFrom;
        private float             m_PlayTo;
        private bool              m_PlayedStateEntered;
        private bool              m_Finished;
        private bool              m_Held;

        private AnimationState m_PushedState;
        private bool           m_HasPushedState;

        private int m_SpeedParameter;
        private int m_FacingXParameter;
        private int m_FacingYParameter;

        private bool m_HasSpeedParameter;
        private bool m_HasFacingParameters;

        internal void Init(Animator animator)
        {
            m_Animator          = animator;
            m_BaseStateName     = null;
            m_BaseStatePending  = false;
            m_ParametersPending = true;
            m_Facing            = Vector3.zero;
            m_SpeedMultiplier   = 1f;
            m_Paused            = false;

            m_PlayingState       = 0;
            m_PlayedStateEntered = false;
            m_Finished           = false;
            m_Held               = false;

            m_HasPushedState = false;

            m_SpeedParameter   = Animator.StringToHash(SPEED_PARAMETER);
            m_FacingXParameter = Animator.StringToHash(FACING_X_PARAMETER);
            m_FacingYParameter = Animator.StringToHash(FACING_Y_PARAMETER);
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

            ApplySpeed();
        }

        void IAnimationDriver.SetPaused(bool paused)
        {
            m_Paused = paused;

            ApplySpeed();
        }

        /// <summary>
        /// A suspended field, an animation holding its last frame and the multiplier a script set all decide the same
        /// field, so they are resolved together rather than overwriting each other.
        /// </summary>
        private void ApplySpeed()
        {
            m_Animator.speed = m_Paused || m_Held ? 0f : m_SpeedMultiplier;
        }

        void IAnimationDriver.Play(string stateName, AnimationPlayMode mode, float from, float to)
        {
            m_PlayingState = Animator.StringToHash(stateName);
            m_PlayMode     = mode;
            m_PlayFrom     = from;
            m_PlayTo       = to;
            m_Finished     = false;
            m_Held         = false;

            m_PlayedStateEntered = false;

            ApplySpeed();

            m_Animator.Play(m_PlayingState, BASE_LAYER, from);
        }

        bool IAnimationDriver.IsPlaying => m_PlayingState != 0 && !m_Finished;

        void IAnimationDriver.PushState()
        {
            m_PushedState = new AnimationState
                            {
                                PlayingState    = m_PlayingState,
                                Mode            = m_PlayMode,
                                From            = m_PlayFrom,
                                To              = m_PlayTo,
                                Finished        = m_Finished,
                                Held            = m_Held,
                                NormalisedTime  = m_Animator.GetCurrentAnimatorStateInfo(BASE_LAYER).normalizedTime,
                                BaseStateName   = m_BaseStateName,
                                SpeedMultiplier = m_SpeedMultiplier
                            };

            m_HasPushedState = true;
        }

        void IAnimationDriver.PopState()
        {
            if (!m_HasPushedState)
            {
                return;
            }

            m_PlayingState    = m_PushedState.PlayingState;
            m_PlayMode        = m_PushedState.Mode;
            m_PlayFrom        = m_PushedState.From;
            m_PlayTo          = m_PushedState.To;
            m_Finished        = m_PushedState.Finished;
            m_Held            = m_PushedState.Held;
            m_BaseStateName   = m_PushedState.BaseStateName;
            m_SpeedMultiplier = m_PushedState.SpeedMultiplier;

            m_HasPushedState = false;

            ApplySpeed();

            if (m_PlayingState != 0)
            {
                m_PlayedStateEntered = false;

                m_Animator.Play(m_PlayingState, BASE_LAYER, m_PushedState.NormalisedTime);
            }
            else
            {
                m_BaseStatePending = true;
            }
        }

        /// <summary>What an entity is playing and where it has got to, kept by PUSH so POP can put it back.</summary>
        private struct AnimationState
        {
            internal int               PlayingState;
            internal AnimationPlayMode Mode;
            internal float             From;
            internal float             To;
            internal bool              Finished;
            internal bool              Held;
            internal float             NormalisedTime;
            internal string            BaseStateName;
            internal float             SpeedMultiplier;
        }

        void IAnimationDriver.ReturnToBase()
        {
            m_PlayingState     = 0;
            m_Held             = false;
            m_BaseStatePending = true;

            ApplySpeed();
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

            AdvancePlayedAnimation();

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

        /// <summary>
        /// Unity advances the clip; what it does not know is where this animation was asked to stop, or what should
        /// happen there. A range ends short of the clip, and a loop is wrapped by hand because its end is not the
        /// clip's.
        /// </summary>
        private void AdvancePlayedAnimation()
        {
            if (m_PlayingState == 0 || m_Held)
            {
                return;
            }

            AnimatorStateInfo state         = m_Animator.GetCurrentAnimatorStateInfo(BASE_LAYER);
            bool              inPlayedState = state.shortNameHash == m_PlayingState;

            // A script plays during the frame's update and the Animator applies it afterwards, so for that frame the
            // layer still reports whatever came before.
            if (!m_PlayedStateEntered)
            {
                if (!inPlayedState)
                {
                    return;
                }

                m_PlayedStateEntered = true;
            }

            // A controller can move on from a state by itself. As far as a script is concerned that is the animation
            // ending — otherwise a wait on it would never finish. Export refuses a state that always does this.
            bool leaving = !inPlayedState ||
                           m_Animator.IsInTransition(BASE_LAYER) && m_Animator.GetNextAnimatorStateInfo(BASE_LAYER).shortNameHash != m_PlayingState;

            if (!leaving && state.normalizedTime < m_PlayTo)
            {
                return;
            }

            m_Finished = true;

            switch (m_PlayMode)
            {
                case AnimationPlayMode.Loop:
                    m_Animator.Play(m_PlayingState, BASE_LAYER, m_PlayFrom);
                    break;

                case AnimationPlayMode.HoldLastFrame:
                    m_Animator.Play(m_PlayingState, BASE_LAYER, m_PlayTo);
                    m_Held = true;
                    ApplySpeed();
                    break;

                case AnimationPlayMode.ReturnToBase:
                    m_PlayingState     = 0;
                    m_BaseStatePending = true;
                    ApplyBaseState();
                    break;
            }
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
