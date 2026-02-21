using UnityEngine;

namespace RPGFramework.Field
{
    public sealed class Rigidbody2DMovementDriver : MonoBehaviour, IMovementDriver
    {
        private Rigidbody2D m_Rigidbody;
        private float       m_Speed;

        private Vector3 m_MoveInput;

        public void Init(Rigidbody2D rb, float speed)
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

            Vector2 velocity = new Vector2(m_MoveInput.x, m_MoveInput.y).normalized * m_Speed;
            m_Rigidbody.MovePosition(m_Rigidbody.position + velocity * deltaTime);
        }

        private void FixedUpdate()
        {
            Tick(Time.fixedDeltaTime);
        }
    }
}