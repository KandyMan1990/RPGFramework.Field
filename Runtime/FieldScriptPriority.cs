namespace RPGFramework.Field
{
    /// <summary>
    /// What each of an entity's priority slots is for: where a script is put when something happens to the
    /// entity. <b>7 is the most urgent</b>; an entity runs its most urgent occupied slot, and a script arriving
    /// in a more urgent one interrupts it until it returns.<br /><br />
    /// A script request can name any slot, and shares it with the event that runs there: a script requested
    /// into <see cref="Interaction" /> keeps the player from talking to the entity until it returns.
    /// </summary>
    public enum FieldScriptPriority : byte
    {
        /// <summary>
        /// The init script, then <see cref="FieldScriptType.Default" /> once every entity's init has run.
        /// </summary>
        Main = 0,

        /// <summary>
        /// No event runs here, so a script requested into it never holds one up.
        /// </summary>
        Unassigned = 1,

        /// <summary>
        /// <see cref="FieldScriptType.OnEnter" />, or <see cref="FieldScriptType.Gateway" />: the player came into
        /// the entity's collision trigger.
        /// </summary>
        Enter = 2,

        /// <summary>
        /// <see cref="FieldScriptType.OnLeave" />: the player left it. Above <see cref="Enter" />, so leaving
        /// interrupts an enter script that is still running, and that script carries on afterwards.
        /// </summary>
        Leave = 3,

        /// <summary>
        /// Kept for a script run on every frame the player is inside the trigger.
        /// </summary>
        Inside = 4,

        /// <summary>
        /// Kept for a script run when the player crosses a line.
        /// </summary>
        Across = 5,

        /// <summary>
        /// Kept for a script run when the player walks into the entity's body.
        /// </summary>
        Push = 6,

        /// <summary>
        /// <see cref="FieldScriptType.OnInteraction" />: the player pressed confirm facing the entity.
        /// </summary>
        Interaction = 7
    }
}
