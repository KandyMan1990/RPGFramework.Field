using System.Collections.Generic;
using RPGFramework.Field.FieldVmArgs;
using RPGFramework.Field.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace RPGFramework.Field
{
    internal sealed class Rigidbody3DMovementDriver : MonoBehaviour, IMovementDriver
    {
        private const float SKIN_WIDTH     = 0.01f;
        private const int   MAX_SLIDES     = 3;
        private const float MIN_MOTION_SQR = 0.000001f;
        private const int   MAX_OVERLAPS   = 16;
        private const float MIN_MOVE_INPUT_SQR = 0.0001f;

        private readonly Collider[] m_Overlaps = new Collider[MAX_OVERLAPS];

        private Collider[] m_SolidColliders;

        private Rigidbody     m_Rigidbody;
        private float         m_Speed;
        private Vector3       m_MoveInput;
        private RotationState m_RotationState;

        public void Init(Rigidbody rb, float speed)
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
                                  Start         = m_Rigidbody.rotation,
                                  Target        = RotationUtility.AdjustDirection(m_Rigidbody.rotation, args.Rotation, args.RotationDirection),
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
                Vector3 currentVelocity = m_MoveInput.sqrMagnitude < MIN_MOVE_INPUT_SQR
                                              ? Vector3.zero
                                              : m_MoveInput.normalized * m_Speed;

                return currentVelocity;
            }
        }

        private void HandleMovement(float deltaTime)
        {
            if (m_MoveInput.sqrMagnitude < MIN_MOVE_INPUT_SQR)
            {
                return;
            }

            Vector3 direction = m_MoveInput.normalized;
            Vector3 origin    = Depenetrate(m_Rigidbody.position);
            Vector3 target    = SweepAndSlide(origin, direction * (m_Speed * deltaTime));

            m_Rigidbody.MovePosition(target);
            m_Rigidbody.MoveRotation(Quaternion.LookRotation(direction));
        }

        private Vector3 SweepAndSlide(Vector3 origin, Vector3 motion)
        {
            Vector3 start    = m_Rigidbody.position;
            Vector3 position = origin;

            for (int slide = 0; slide < MAX_SLIDES && motion.sqrMagnitude > MIN_MOTION_SQR; slide++)
            {
                m_Rigidbody.position = position;

                float   distance = motion.magnitude;
                Vector3 heading  = motion / distance;

                if (!m_Rigidbody.SweepTest(heading, out RaycastHit hit, distance + SKIN_WIDTH, QueryTriggerInteraction.Ignore))
                {
                    position += motion;

                    break;
                }

                float travelled = Mathf.Max(hit.distance - SKIN_WIDTH, 0f);

                position += heading * travelled;
                motion   =  Vector3.ProjectOnPlane(motion - heading * travelled, hit.normal);
            }

            m_Rigidbody.position = start;

            return position;
        }

        private Vector3 Depenetrate(Vector3 position)
        {
            foreach (Collider own in m_SolidColliders)
            {
                Bounds bounds = own.bounds;

                int count = Physics.OverlapBoxNonAlloc(bounds.center, bounds.extents, m_Overlaps, Quaternion.identity, Physics.AllLayers, QueryTriggerInteraction.Ignore);

                Vector3 offset = own.transform.position - m_Rigidbody.position;

                for (int i = 0; i < count; i++)
                {
                    Collider other = m_Overlaps[i];

                    if (other.attachedRigidbody == m_Rigidbody)
                    {
                        continue;
                    }

                    Transform otherTransform = other.transform;

                    if (!Physics.ComputePenetration(own, position + offset, own.transform.rotation, other, otherTransform.position, otherTransform.rotation, out Vector3 outward, out float depth))
                    {
                        continue;
                    }

                    position += outward * (depth + SKIN_WIDTH);
                }
            }

            return position;
        }

        private static Collider[] GatherSolidColliders(Rigidbody body)
        {
            List<Collider> solid = new List<Collider>();

            foreach (Collider collider in body.GetComponentsInChildren<Collider>(true))
            {
                if (collider.isTrigger || collider.attachedRigidbody != body)
                {
                    continue;
                }

                solid.Add(collider);
            }

            return solid.ToArray();
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

            Quaternion rot = Quaternion.Slerp(m_RotationState.Start, m_RotationState.Target, t);

            m_Rigidbody.MoveRotation(rot);

            if (m_RotationState.Elapsed >= m_RotationState.Duration)
            {
                m_Rigidbody.MoveRotation(m_RotationState.Target);

                m_RotationState.Active = false;
            }
        }
    }
}