namespace RPGFramework.Field
{
    /// <summary>
    /// What becomes of a played animation when it reaches its end.
    /// </summary>
    internal enum AnimationPlayMode
    {
        /// <summary>Go back to the entity's base animation.</summary>
        ReturnToBase = 0,

        /// <summary>Stay on the last frame until something else plays.</summary>
        HoldLastFrame = 1,

        /// <summary>Start again. It still reports having finished each time round, so a wait on it ends.</summary>
        Loop = 2
    }
}
