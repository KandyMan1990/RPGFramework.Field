using UnityEngine;

namespace RPGFramework.Field
{
    public sealed class TransformMovementDriver : MonoBehaviour, IMovementDriver
    {
        private Transform m_Transform;
        private float     m_Speed;

        private Vector3 m_MoveInput;

        public void Init(Transform entityTransform, float speed)
        {
            m_Transform = entityTransform;
            m_Speed     = speed;
        }

        public void SetMoveInput(Vector3 worldMove)
        {
            m_MoveInput = worldMove;
        }

        public void Tick(float deltaTime)
        {
            if (m_MoveInput.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Vector3 direction = m_MoveInput.normalized;

            m_Transform.position += direction * (m_Speed * deltaTime);
            m_Transform.forward  =  direction;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }
    }
}