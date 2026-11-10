using Unity.Mathematics;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// How far the player's body really moved each frame, and how long that is at its speed. A body moved by physics
    /// moves only in the physics step, so a frame can show none of its walk and the next more than a frame's: measured by
    /// distance, it adds up whatever the frame rate. Never more than a frame and a physics step's walk, so being put
    /// somewhere is no long walk.
    /// </summary>
    internal sealed class PlayerWalk
    {
        private Vector3 m_Last;

        internal PlayerWalk()
        {
            m_Last = Vector3.zero;
        }

        internal PlayerWalkArgs Take(Vector3 position, float speed, float deltaTime, float fixedDeltaTime)
        {
            float moved = math.distance(m_Last, position);

            m_Last = position;

            float distance = math.min(moved, speed * (deltaTime + fixedDeltaTime));
            float seconds  = speed > 0f ? distance / speed : 0f;

            return new PlayerWalkArgs(seconds, distance, position);
        }
    }
}
