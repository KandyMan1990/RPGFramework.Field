namespace RPGFramework.Field.Tests.Vm
{
    /// <summary>Where an argument comes from, as a sources byte's nibble names it.</summary>
    internal enum Source : byte
    {
        Immediate  = 0,
        Persistent = 1,
        Session    = 2,
        Temp       = 3
    }
}
