namespace RPGFramework.Field.Tests.Encounters
{
    /// <summary>The meter's state, held in the fake, the rate at 1 to start.</summary>
    internal sealed class FakeFieldEncounterStore : IFieldEncounterStore
    {
        internal FakeFieldEncounterStore()
        {
            Danger         = 0;
            StepFraction   = 0f;
            LastEnemyGroup = 0;
            Rate           = 1f;
        }

        internal ushort Danger         { get; set; }
        internal float  StepFraction   { get; set; }
        internal ushort LastEnemyGroup { get; set; }
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

        ushort IFieldEncounterStore.LastEnemyGroup => LastEnemyGroup;

        void IFieldEncounterStore.SetLastEnemyGroup(ushort group)
        {
            LastEnemyGroup = group;
        }

        float IFieldEncounterStore.Rate => Rate;
    }
}
