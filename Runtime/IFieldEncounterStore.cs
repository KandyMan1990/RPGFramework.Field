namespace RPGFramework.Field
{
    /// <summary>
    /// How near the next random battle is, kept across fields and a scripted battle but not in the save, and what scales
    /// every field's rate, which is.
    /// </summary>
    internal interface IFieldEncounterStore
    {
        /// <summary>Rises with each check the player walks into; a random battle clears it.</summary>
        ushort Danger { get; }

        /// <summary>Sets the danger.</summary>
        void SetDanger(ushort danger);

        /// <summary>How far the player has walked toward the next check, from 0 to 1.</summary>
        float StepFraction { get; }

        /// <summary>Sets how far the player has walked toward the next check.</summary>
        void SetStepFraction(float fraction);

        /// <summary>The enemy group the last random battle fought, which the next does not repeat where it can help it.</summary>
        ushort LastEnemyGroup { get; }

        /// <summary>Sets the enemy group the last random battle fought.</summary>
        void SetLastEnemyGroup(ushort group);

        /// <summary>Multiplies every field's rate: 1 as authored, 0.5 half as often, 0 none.</summary>
        float Rate { get; }
    }
}
