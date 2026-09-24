using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// What plays an entity's animation. Separate from <see cref="IMovementDriver" /> because an entity can animate
    /// without moving and move without animating, and a game supplies its own rig either way.
    /// </summary>
    internal interface IAnimationDriver
    {
        /// <summary>
        /// The state an entity rests in, and returns to when nothing else is playing. It names a state rather than a
        /// clip because locomotion is usually a blend tree: the driver reports how fast the entity is moving and the
        /// game's own controller decides where between standing and running that falls.
        /// </summary>
        void SetBaseAnimation(string stateName);

        /// <summary>
        /// Scales playback, 1 being as authored. A multiplier rather than a rate, so it holds at any frame rate.
        /// </summary>
        void SetSpeedMultiplier(float multiplier);

        /// <summary>
        /// Play an animation over the top of the base one. <paramref name="from" /> and <paramref name="to" /> are
        /// normalised, 0 the beginning and 1 the end, so a range means the same thing whatever the clip's length.
        /// </summary>
        void Play(string stateName, AnimationPlayMode mode, float from, float to);

        /// <summary>
        /// False once the played animation has reached its end, which a looping one does on every pass — so a script
        /// waiting on a loop waits one time round rather than forever.
        /// </summary>
        bool IsPlaying { get; }

        void ReturnToBase();

        /// <summary>
        /// Remember what is playing and where it has got to, so an interruption can put it back.
        /// </summary>
        void PushState();

        void PopState();

        /// <summary>
        /// A suspended field freezes. Unity advances an Animator itself, so unlike movement — which stops simply by
        /// not being ticked — animation has to be told.
        /// </summary>
        void SetPaused(bool paused);

        /// <summary>
        /// <paramref name="velocity" /> is how the entity is actually moving this frame, zero when it is still. The
        /// driver takes both how fast it is going and which way it faces from it.
        /// </summary>
        void Tick(Vector3 velocity);
    }
}
