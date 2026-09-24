namespace RPGFramework.Field.FieldVmArgs
{
    /// <summary>
    /// One request to play an animation over an entity's base one. <see cref="From" /> and <see cref="To" /> are
    /// normalised, 0 the beginning and 1 the end, so a range means the same whatever the clip's length.
    /// </summary>
    internal readonly struct PlayAnimationArgs
    {
        internal readonly ulong             NameHash;
        internal readonly AnimationPlayMode Mode;
        internal readonly float             From;
        internal readonly float             To;

        internal PlayAnimationArgs(ulong nameHash, AnimationPlayMode mode, float from, float to)
        {
            NameHash = nameHash;
            Mode     = mode;
            From     = from;
            To       = to;
        }
    }
}
