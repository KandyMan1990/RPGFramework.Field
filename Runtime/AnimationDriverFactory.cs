using UnityEngine;

namespace RPGFramework.Field
{
    internal static class AnimationDriverFactory
    {
        /// <summary>
        /// The Animator sits on the visuals a game supplies, not on the body the framework builds, so it is looked
        /// for in the children — inactive ones included, because an entity's visuals start hidden and are shown by
        /// <c>VISIBILITY</c>. Export refuses an animation opcode on an entity whose body has none.
        /// </summary>
        internal static IAnimationDriver Create(GameObject gameObject)
        {
            Animator animator = gameObject.GetComponentInChildren<Animator>(true);

            AnimatorAnimationDriver animationDriver = gameObject.AddComponent<AnimatorAnimationDriver>();
            animationDriver.Init(animator);

            return animationDriver;
        }
    }
}
