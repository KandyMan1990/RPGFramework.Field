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
        /// loop, on its own priority slot so a trigger firing does not disturb it.
        /// </summary>
        Main = 0,

        /// <summary>
        /// Setup, run at field load before the field is shown: position, model, visibility, solidity.
        /// Must be an entity's first script and must run straight through — anything that waits belongs
        /// in <see cref="Main" />.
        /// </summary>
        Init = 1,

        OnCollision = 2,

        OnInteraction = 3
    }
}
