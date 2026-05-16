using RPGFramework.Field.FieldVmArgs;
using RPGFramework.Field.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace RPGFramework.Field
{
    public sealed class Rigidbody3DMovementDriver : MonoBehaviour, IMovementDriver
    {
        private Rigidbody       m_Rigidbody;
        private float           m_Speed;
        private IMovementDriver m_This;
        private Vector3         m_MoveInput;
        private RotationState   m_RotationState;

        public void Init(Rigidbody rb, float speed)
        {
            m_Rigidbody     = rb;
            m_Speed         = speed;
            m_This          = this;
            m_RotationState = default;
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
            HandleMovement(deltaTime);
            HandleRotation(deltaTime);
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

        private void FixedUpdate()
        {
            m_This.Tick(Time.fixedDeltaTime);
        }

        private void HandleMovement(float deltaTime)
        {
            if (m_MoveInput.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Vector3 direction = m_MoveInput.normalized;
            Vector3 velocity  = direction * m_Speed;

            m_Rigidbody.MovePosition(m_Rigidbody.position + velocity * deltaTime);
            m_Rigidbody.transform.forward = direction;
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