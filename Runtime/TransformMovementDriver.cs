using RPGFramework.Field.FieldVmArgs;
using RPGFramework.Field.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace RPGFramework.Field
{
    public sealed class TransformMovementDriver : MonoBehaviour, IMovementDriver
    {
        private Transform       m_Transform;
        private float           m_Speed;
        private IMovementDriver m_This;
        private Vector3         m_MoveInput;
        private RotationState   m_RotationState;

        public void Init(Transform entityTransform, float speed)
        {
            m_Transform     = entityTransform;
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
            m_Transform.position = position;
        }

        void IMovementDriver.SetRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
        }

        void IMovementDriver.StartRotation(SetEntityRotationAsyncArgs args)
        {
            m_RotationState = new RotationState
                              {
                                      Active        = true,
                                      Start         = m_Transform.rotation,
                                      Target        = RotationUtility.AdjustDirection(m_Transform.rotation, args.Rotation, args.RotationDirection),
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

        private void Update()
        {
            m_This.Tick(Time.deltaTime);
        }

        private void HandleMovement(float deltaTime)
        {
            if (m_MoveInput.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Vector3 direction = m_MoveInput.normalized;

            m_Transform.position += direction * (m_Speed * deltaTime);
            m_Transform.forward  =  direction;
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

            m_Transform.rotation = Quaternion.Slerp(m_RotationState.Start, m_RotationState.Target, t);

            if (m_RotationState.Elapsed >= m_RotationState.Duration)
            {
                m_Transform.rotation = m_RotationState.Target;

                m_RotationState.Active = false;
            }
        }
    }
}