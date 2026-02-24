using System.Threading.Tasks;
using RPGFramework.Field.FieldVmArgs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RPGFramework.Field
{
    public sealed class TilemapMovementDriver : MonoBehaviour, IMovementDriver
    {
        private Transform       m_Transform;
        private Tilemap         m_Tilemap;
        private float           m_Speed;
        private IMovementDriver m_This;

        private Vector3 m_Target;
        private bool    m_Moving;

        public void Init(Transform entityTransform, Tilemap tilemap, float speed)
        {
            m_Transform = entityTransform;
            m_Tilemap   = tilemap;
            m_Speed     = speed;
            m_This      = this;
        }

        void IMovementDriver.SetMoveInput(Vector3 move)
        {
            if (m_Moving)
            {
                return;
            }

            Vector3Int cell = m_Tilemap.WorldToCell(m_Transform.position);

            Vector3Int dir = Quantize(move);

            if (dir == Vector3Int.zero)
            {
                return;
            }

            Vector3Int next = cell + dir;

            if (!m_Tilemap.HasTile(next))
            {
                return;
            }

            m_Target = m_Tilemap.GetCellCenterWorld(next);
            m_Moving = true;
        }

        void IMovementDriver.SetMoveSpeed(float speed)
        {
            m_Speed = speed;
        }

        void IMovementDriver.Tick(float deltaTime)
        {
            if (!m_Moving)
            {
                return;
            }

            m_Transform.position = Vector3.MoveTowards(m_Transform.position, m_Target, m_Speed * deltaTime);

            Vector3 dir = m_Target - m_Transform.position;
            if (dir.sqrMagnitude > 0.0001f)
            {
                m_Transform.forward = dir.normalized;
            }

            if (Vector3.Distance(m_Transform.position, m_Target) < 0.001f)
            {
                m_Transform.position = m_Target;
                m_Moving             = false;
            }
        }

        void IMovementDriver.SetPosition(Vector3 position)
        {
            m_Transform.position = position;
        }

        void IMovementDriver.SetRotation(Quaternion rotation)
        {
            m_Transform.rotation = rotation;
        }

        async Task IMovementDriver.SetRotationAsync(SetEntityRotationAsyncArgs args)
        {
            Quaternion start  = m_Transform.rotation;
            Quaternion target = ResolveDirection(start, args.Rotation, args.RotationDirection);

            float elapsed = 0f;

            while (elapsed < args.Duration)
            {
                elapsed += Time.deltaTime;

                float t = math.clamp(elapsed / args.Duration, 0f, 1f);

                if (args.RotationType == RotationInterpolation.Smooth)
                {
                    t = math.smoothstep(0f, 1f, t);
                }

                m_Transform.rotation = Quaternion.Slerp(start, target, t);

                await Awaitable.NextFrameAsync();
            }

            m_Transform.rotation = target;
        }

        private static Vector3Int Quantize(Vector3 move)
        {
            if (move.sqrMagnitude < 0.0001f)
            {
                return Vector3Int.zero;
            }

            move.Normalize();

            if (Mathf.Abs(move.x) > Mathf.Abs(move.z))
            {
                return move.x > 0 ? Vector3Int.right : Vector3Int.left;
            }

            return move.z > 0 ? Vector3Int.up : Vector3Int.down;
        }

        private static Quaternion ResolveDirection(Quaternion start, Quaternion target, RotationDirection direction)
        {
            if (direction == RotationDirection.Closest)
            {
                return target;
            }

            float angle = Quaternion.Angle(start, target);

            if (angle < 0.001f)
            {
                return target;
            }

            Quaternion delta = target * Quaternion.Inverse(start);
            delta.ToAngleAxis(out float deltaAngle, out Vector3 axis);

            float signedAngle = Vector3.Dot(axis, Vector3.up) < 0 ? -deltaAngle : deltaAngle;

            bool clockwise = signedAngle < 0;

            if (direction == RotationDirection.Clockwise && !clockwise)
            {
                target = Quaternion.AngleAxis(-(360 - deltaAngle), Vector3.up) * start;
            }
            else if (direction == RotationDirection.CounterClockwise && clockwise)
            {
                target = Quaternion.AngleAxis(360 - deltaAngle, Vector3.up) * start;
            }

            return target;
        }

        private void Update()
        {
            m_This.Tick(Time.deltaTime);
        }
    }
}