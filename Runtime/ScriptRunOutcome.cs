namespace RPGFramework.Field
{
    /// <summary>
    /// Why a slot stopped running this frame.
    /// </summary>
    internal enum ScriptRunOutcome
    {
        /// <summary>The script returned, or could not run, and its slot is free.</summary>
        Ended,

        /// <summary>The script is waiting on something, or yielded the rest of the frame.</summary>
        Waiting,

        /// <summary>The script used its instruction budget and will carry on where it stopped.</summary>
        OutOfInstructions
    }
}