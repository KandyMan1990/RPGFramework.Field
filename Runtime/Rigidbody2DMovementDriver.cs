using System.Collections.Generic;
using RPGFramework.Field.FieldVmArgs;
using RPGFramework.Field.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace RPGFramework.Field
{
    internal sealed class Rigidbody2DMovementDriver : MonoBehaviour, IMovementDriver
    {
        private const float SKIN_WIDTH     = 0.01f;
        private const int   MAX_SLIDES     = 3;
        private const float MIN_MOTION_SQR = 0.000001f;
        private const float MIN_MOVE_INPUT_SQR = 0.0001f;

        private static readonly ContactFilter2D SOLID_ONLY = new ContactFilter2D { useTriggers = false };

        private readonly List<RaycastHit2D> m_Hits     = new List<RaycastHit2D>(8);
        private readonly List<Collider2D>   m_Overlaps = new List<Collider2D>(16);

        private Collider2D[] m_SolidColliders;

        private Rigidbody2D   m_Rigidbody;
        private float         m_Speed;
        private Vector3       m_MoveInput;
        private RotationState m_RotationState;
        private ScriptedMove  m_ScriptedMove;

        public void Init(Rigidbody2D rb, float speed)
        {
            m_Rigidbody      = rb;
            m_Speed          = speed;
            m_RotationState  = default;
            m_SolidColliders = GatherSolidColliders(rb);
        }

        void IMovementDriver.SetMoveInput(Vector3 worldMove)
        {
            m_MoveInput = worldMove;
        }

        void IMovementDriver.SetMoveSpeed(float speed)
        {
            m_Speed = speed;
        }

        void IMovementDriver.Tick(float deltaTime)
        {
            // noop - this driver moves a rigidbody, which is physics-step work.
        }

        void IMovementDriver.PhysicsTick(float fixedDeltaTime)
        {
            HandleMovement(fixedDeltaTime);
            HandleRotation(fixedDeltaTime);
        }

        void IMovementDriver.SetPosition(Vector3 position)
        {
            Transform bodyTransform = m_Rigidbody.transform;

            m_Rigidbody.position   = position;
            bodyTransform.position = new Vector3(position.x, position.y, bodyTransform.position.z);
        }

        void IMovementDriver.SetRotation(Quaternion rotation)
        {
            m_Rigidbody.rotation           = rotation.eulerAngles.z;
            m_Rigidbody.transform.rotation = rotation;
        }

        void IMovementDriver.StartRotation(SetEntityRotationAsyncArgs args)
        {
            m_RotationState = new RotationState
                              {
                                  Active        = true,
                                  Start         = new Quaternion(0f, 0f, m_Rigidbody.rotation,        0f),
                                  Target        = new Quaternion(0f, 0f, args.Rotation.eulerAngles.z, 0f),
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

        Vector3 IMovementDriver.CurrentVelocity
        {
            get
            {
                Vector3 heading = m_ScriptedMove.Active ? m_ScriptedMove.Target - (Vector3)m_Rigidbody.position : m_MoveInput;

                Vector3 currentVelocity = heading.sqrMagnitude < MIN_MOVE_INPUT_SQR
                                              ? Vector3.zero
                                              : heading.normalized * m_Speed;

                return currentVelocity;
            }
        }

        void IMovementDriver.MoveTo(Vector3 target, float stopDistance, bool faceTravel)
        {
            m_ScriptedMove = new ScriptedMove
                             {
                                 Active       = true,
                                 Target       = new Vector3(target.x, target.y, 0f),
                                 StopDistance = stopDistance,
                                 FaceTravel   = faceTravel
                             };
        }

        void IMovementDriver.StopMove()
        {
            m_ScriptedMove.Active = false;
        }

        bool IMovementDriver.IsMovingToTarget => m_ScriptedMove.Active;

        private void HandleMovement(float deltaTime)
        {
            if (m_ScriptedMove.Active)
            {
                HandleScriptedMove(deltaTime);

                return;
            }

            if (m_MoveInput.sqrMagnitude < MIN_MOVE_INPUT_SQR)
            {
                return;
            }

            Vector2 velocity = new Vector2(m_MoveInput.x, m_MoveInput.y).normalized * m_Speed;
            Vector2 origin   = Depenetrate(m_Rigidbody.position);
            Vector2 target   = SweepAndSlide(origin, velocity * deltaTime);

            m_Rigidbody.MovePosition(target);
        }

        private void HandleScriptedMove(float deltaTime)
        {
            Vector2 origin = Depenetrate(m_Rigidbody.position);
            Vector2 motion = m_ScriptedMove.Motion(origin, m_Speed * deltaTime);
            Vector2 target = SweepAndSlide(origin, motion);

            m_Rigidbody.MovePosition(target);

            if (m_ScriptedMove.HasArrived(target))
            {
                m_ScriptedMove.Active = false;
            }
        }

        private Vector2 SweepAndSlide(Vector2 origin, Vector2 motion)
        {
            Vector2 start    = m_Rigidbody.position;
            Vector2 position = origin;

            for (int slide = 0; slide < MAX_SLIDES && motion.sqrMagnitude > MIN_MOTION_SQR; slide++)
            {
                m_Rigidbody.position = position;

                float   distance = motion.magnitude;
                Vector2 heading  = motion / distance;

                int hitCount = m_Rigidbody.Cast(heading, SOLID_ONLY, m_Hits, distance + SKIN_WIDTH);

                if (!TryGetNearestHit(hitCount, out RaycastHit2D nearest))
                {
                    position += motion;

                    break;
                }

                float travelled = Mathf.Max(nearest.distance - SKIN_WIDTH, 0f);

                position += heading * travelled;

                Vector2 remaining = motion - heading * travelled;

                motion = remaining - Vector2.Dot(remaining, nearest.normal) * nearest.normal;
            }

            m_Rigidbody.position = start;

            return position;
        }

        private Vector2 Depenetrate(Vector2 position)
        {
            foreach (Collider2D own in m_SolidColliders)
            {
                int count = own.Overlap(SOLID_ONLY, m_Overlaps);

                for (int i = 0; i < count; i++)
                {
                    Collider2D other = m_Overlaps[i];

                    if (other.attachedRigidbody == m_Rigidbody || !FieldEntity.Blocks(other))
                    {
                        continue;
                    }

                    ColliderDistance2D separation = own.Distance(other);

                    if (!separation.isOverlapped)
                    {
                        continue;
                    }

                    Vector2 push = separation.pointB - separation.pointA;

                    position += push + push.normalized * SKIN_WIDTH;
                }
            }

            return position;
        }

        private static Collider2D[] GatherSolidColliders(Rigidbody2D body)
        {
            List<Collider2D> solid = new List<Collider2D>();

            foreach (Collider2D collider in body.GetComponentsInChildren<Collider2D>(true))
            {
                if (collider.isTrigger || collider.attachedRigidbody != body)
                {
                    continue;
                }

                solid.Add(collider);
            }

            return solid.ToArray();
        }

        private bool TryGetNearestHit(int hitCount, out RaycastHit2D nearest)
        {
            nearest = default;

            bool found = false;

            for (int i = 0; i < hitCount; i++)
            {
                if (!FieldEntity.Blocks(m_Hits[i].collider) || found && m_Hits[i].distance >= nearest.distance)
                {
                    continue;
                }

                nearest = m_Hits[i];
                found   = true;
            }

            return found;
        }

        private void HandleRotation(float deltaTime)
        {
            if (!m_RotationState.Active)
            {
                return;
            }

            m_RotationState.Elapsed += deltaTime;

            float t = math.clamp(m_RotationState.Elapsed / m_RotationState.Duration, 0f, 1f);
            t = RotationUtility.ApplyInterpolation(t, m_RotationState.Interpolation);

            float rot = MathUtils.LerpAngle(m_RotationState.Start.z, m_RotationState.Target.z, t);

            m_Rigidbody.MoveRotation(rot);

            if (m_RotationState.Elapsed >= m_RotationState.Duration)
            {
                m_Rigidbody.MoveRotation(m_RotationState.Target.z);

                m_RotationState.Active = false;
            }
        }
    }
}