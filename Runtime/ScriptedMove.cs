using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// A point a script has sent an entity to. Each movement driver steps toward it in place of the player's input,
    /// so the arithmetic of approaching and arriving lives here rather than in four drivers.
    /// </summary>
    internal struct ScriptedMove
    {
        private const float ARRIVAL_DISTANCE = 0.001f;

        internal bool    Active;
        internal Vector3 Target;
        internal float   StopDistance;
        internal bool    FaceTravel;

        /// <summary>
        /// How far to move toward the target this step, going no further than <paramref name="maxDistance" /> and
        /// stopping <see cref="StopDistance" /> short of it.
        /// </summary>
        internal readonly Vector3 Motion(Vector3 position, float maxDistance)
        {
            Vector3 toTarget = Target - position;
            float   distance = toTarget.magnitude;
            float   travel   = Mathf.Min(distance - StopDistance, maxDistance);

            Vector3 motion = travel > 0f ? toTarget / distance * travel : Vector3.zero;

            return motion;
        }

        /// <summary>
        /// Judged from where the body ended up rather than where it was sent, so one blocked short of the target is
        /// still travelling and keeps trying, as the reference's does.
        /// </summary>
        internal readonly bool HasArrived(Vector3 position)
        {
            bool hasArrived = (Target - position).magnitude <= StopDistance + ARRIVAL_DISTANCE;

            return hasArrived;
        }
    }
}
