using UnityEngine;

namespace RPGFramework.Field
{
    public sealed class Rigidbody3DMovementDriver : MonoBehaviour, IMovementDriver
    {
        private Rigidbody m_Rigidbody;
        private float     m_Speed;

        private Vector3 m_MoveInput;

        public void Init(Rigidbody rb, float speed)
        {
            m_Rigidbody = rb;
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
            Vector3 velocity  = direction * m_Speed;

            m_Rigidbody.MovePosition(m_Rigidbody.position + velocity * deltaTime);
            m_Rigidbody.transform.forward = direction;
        }

        private void FixedUpdate()
        {
            Tick(Time.fixedDeltaTime);
        }
    }
}