namespace RPGFramework.Field
{
    /// <summary>
    /// What an opcode does, the framework's and a game's alike: read every argument from the running script, in the order
    /// the opcode's <see cref="ArgumentAttribute" />s number them, then act. The VM holds one table of them, indexed by the
    /// opcode.
    /// </summary>
    public delegate void FieldOpCodeHandler(FieldScriptContext context);
}
