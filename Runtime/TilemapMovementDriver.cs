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
        private Vector3         m_Target;
        private bool            m_Moving;
        private RotationState   m_RotationState;

        public void Init(Transform entityTransform, Tilemap tilemap, float speed)
        {
            m_Transform     = entityTransform;
            m_Tilemap       = tilemap;
            m_Speed         = speed;
            m_This          = this;
            m_RotationState = default;
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
            HandleMovement(deltaTime);
            HandleRotation(deltaTime);
        }

        void IMovementDriver.SetPosition(Vector3 position)
        {
            m_Transform.position = position;
        }

        void IMovementDriver.SetRotation(Quaternion rotation)
        {
            m_Transform.rotation = rotation;
        }

        void IMovementDriver.StartRotation(SetEntityRotationAsyncArgs args)
        {
            m_RotationState = new RotationState
                              {
                                      Active        = true,
                                      Start         = m_Transform.rotation,
                                      Target        = ResolveDirection(m_Transform.rotation, args.Rotation, args.RotationDirection),
                                      Elapsed       = 0f,
                                      Duration      = args.Duration,
                                      Interpolation = args.RotationType
                              };
        }

        void IMovementDriver.ResumeRotation(RotationState rotationState)
        {
            m_RotationState = rotationState;
        }

        RotationState IMovementDriver.GetRotationState()
        {
            return m_RotationState;
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

        private void HandleMovement(float deltaTime)
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

        private void HandleRotation(float deltaTime)
        {
            if (!m_RotationState.Active)
            {
                return;
            }

            m_RotationState.Elapsed += deltaTime;

            float t = math.clamp(m_RotationState.Elapsed / m_RotationState.Duration, 0f, 1f);
            if (m_RotationState.Interpolation == RotationInterpolation.Smooth)
            {
                t = math.smoothstep(0f, 1f, t);
            }

            m_Transform.rotation = Quaternion.Slerp(m_RotationState.Start, m_RotationState.Target, t);

            if (m_RotationState.Elapsed >= m_RotationState.Duration)
            {
                m_Transform.rotation = m_RotationState.Target;

                m_RotationState.Active = false;
            }
        }
    }
}