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

        private static readonly ContactFilter2D SOLID_ONLY = new ContactFilter2D { useTriggers = false };

        private readonly List<RaycastHit2D> m_Hits     = new List<RaycastHit2D>(8);
        private readonly List<Collider2D>   m_Overlaps = new List<Collider2D>(16);

        private Collider2D[] m_SolidColliders;

        private Rigidbody2D   m_Rigidbody;
        private float         m_Speed;
        private Vector3       m_MoveInput;
        private RotationState m_RotationState;

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
            m_Rigidbody.MovePosition(position);
        }

        void IMovementDriver.SetRotation(Quaternion rotation)
        {
            m_Rigidbody.MoveRotation(rotation);
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

        private void HandleMovement(float deltaTime)
        {
            if (m_MoveInput.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Vector2 velocity = new Vector2(m_MoveInput.x, m_MoveInput.y).normalized * m_Speed;
            Vector2 origin   = Depenetrate(m_Rigidbody.position);
            Vector2 target   = SweepAndSlide(origin, velocity * deltaTime);

            m_Rigidbody.MovePosition(target);
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

                if (hitCount == 0)
                {
                    position += motion;

                    break;
                }

                RaycastHit2D nearest = NearestHit(hitCount);

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

                    if (other.attachedRigidbody == m_Rigidbody)
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

        private RaycastHit2D NearestHit(int hitCount)
        {
            RaycastHit2D nearest = m_Hits[0];

            for (int i = 1; i < hitCount; i++)
            {
                if (m_Hits[i].distance < nearest.distance)
                {
                    nearest = m_Hits[i];
                }
            }

            return nearest;
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