namespace RPGFramework.Field.Tests.Encounters
{
    /// <summary>The meter's state, held in the fake, the rate at 1 to start.</summary>
    internal sealed class FakeFieldEncounterStore : IFieldEncounterStore
    {
        internal FakeFieldEncounterStore()
        {
            Danger         = 0;
            StepFraction   = 0f;
            LastEncounter  = 0;
            Rate           = 1f;
        }

        internal ushort Danger         { get; set; }
        internal float  StepFraction   { get; set; }
        internal ulong  LastEncounter  { get; set; }
        internal float  Rate           { get; set; }

        ushort IFieldEncounterStore.Danger => Danger;

        void IFieldEncounterStore.SetDanger(ushort danger)
        {
            Danger = danger;
        }

        float IFieldEncounterStore.StepFraction => StepFraction;

        void IFieldEncounterStore.SetStepFraction(float fraction)
        {
            StepFraction = fraction;
        }

        ulong IFieldEncounterStore.LastEncounter => LastEncounter;

        void IFieldEncounterStore.SetLastEncounter(ulong encounter)
        {
            LastEncounter = encounter;
        }

        float IFieldEncounterStore.Rate => Rate;
    }
}
