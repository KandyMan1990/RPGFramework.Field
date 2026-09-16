namespace RPGFramework.Field
{
    /// <summary>
    /// Why a slot stopped running this frame.
    /// </summary>
    internal enum ScriptRunOutcome
    {
        /// <summary>The script returned, handed its slot to another script, or could not run.</summary>
        Ended,

        /// <summary>A more urgent slot on the same entity was filled, so that slot runs next.</summary>
        Preempted,

        /// <summary>The script is waiting on something, or yielded the rest of the frame.</summary>
        Waiting,

        /// <summary>The script used its instruction budget and will carry on where it stopped.</summary>
        OutOfInstructions
    }
}