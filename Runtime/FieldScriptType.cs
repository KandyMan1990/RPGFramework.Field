namespace RPGFramework.Field
{
    /// <summary>
    /// What an entity's script is for. The position of a script in an entity's list is its
    /// <b>event id</b> — what a script-request opcode names — and these say what each position means.
    /// </summary>
    public enum FieldScriptType
    {
        /// <summary>
        /// The entity's ongoing behaviour: a patrol route, an idle animation loop, anything that runs
        /// for as long as the field does. Started once initialisation is complete and free to yield and
        /// loop, in the least urgent slot so any trigger preempts it and it resumes afterwards.
        /// </summary>
        Main = 0,

        /// <summary>
        /// Setup, run at field load before the field is shown: position, model, visibility, solidity.
        /// Must be an entity's first script and must run straight through — anything that waits belongs
        /// in <see cref="Main" />.
        /// </summary>
        Init = 1,

        /// <summary>
        /// Run when the player comes into the entity's collision trigger.
        /// </summary>
        OnEnter = 2,

        OnInteraction = 3,

        /// <summary>
        /// Run when the player leaves the entity's collision trigger: the counterpart of
        /// <see cref="OnEnter" />, switched off by the same things.
        /// </summary>
        OnLeave = 4,

        /// <summary>
        /// A way out of the field: run when the player comes into the entity's collision trigger, as
        /// <see cref="OnEnter" /> is, but only while <c>GATEWAY_TRIGGER_ACTIVATION</c> has gateways on. It is where
        /// the <c>JUMP_TO_MAP</c> that leaves goes.
        /// </summary>
        Gateway = 5
    }
}