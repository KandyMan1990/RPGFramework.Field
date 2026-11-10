using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>How the player walked in one frame, for an <see cref="IFieldEncounterDriver" />.</summary>
    public readonly struct PlayerWalkArgs
    {
        /// <summary>How long it walked, at its own speed.</summary>
        public float Seconds { get; }

        /// <summary>How far it walked, in the field's units.</summary>
        public float Distance { get; }

        /// <summary>Where it walked to.</summary>
        public Vector3 Position { get; }

        internal PlayerWalkArgs(float seconds, float distance, Vector3 position)
        {
            Seconds  = seconds;
            Distance = distance;
            Position = position;
        }
    }
}
