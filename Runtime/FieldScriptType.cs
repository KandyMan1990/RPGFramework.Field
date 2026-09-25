namespace RPGFramework.Field
{
    /// <summary>
    /// What an entity's script is for. The position of a script in an entity's list is its
    /// <b>event id</b> — what a script-request opcode names — and these say what each position means.
    /// </summary>
    public enum FieldScriptType
    {
        /// <summary>
        /// Setup, run at field load before the field is shown: position, model, visibility, solidity.
        /// Must be an entity's first script and must run straight through — anything that waits belongs
        /// in <see cref="Default" />.
        /// </summary>
        Init = 0,

        /// <summary>
        /// The entity's ongoing behaviour: a patrol route, an idle animation loop, anything that runs
        /// for as long as the field does. Started once initialisation is complete and free to yield and
        /// loop, in the least urgent slot so any trigger preempts it and it resumes afterwards.
        /// </summary>
        Default = 1,

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
        Gateway = 5,

        /// <summary>
        /// A script nothing raises: it runs only when another script asks for it by event id, with
        /// <c>REQUEST_SCRIPT</c> and its waiting forms. An entity may hold as many as it likes — a shop, a step
        /// of a cutscene, anything shared between its other scripts.
        /// </summary>
        Requested = 6,

        /// <summary>
        /// Run when the player walks into the entity's body — an NPC reacting to being bumped, a door that opens when
        /// walked into — and again each time it returns while the player keeps pushing. Unlike
        /// <see cref="OnEnter" />, which is a trigger volume, this is the body itself.
        /// </summary>
        OnPush = 7
    }
}