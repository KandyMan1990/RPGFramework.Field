using System.Threading.Tasks;
using RPGFramework.Field.FieldVmArgs;
using RPGFramework.Field.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace RPGFramework.Field
{
    public sealed class Rigidbody2DMovementDriver : MonoBehaviour, IMovementDriver
    {
        private Rigidbody2D     m_Rigidbody;
        private float           m_Speed;
        private IMovementDriver m_This;

        private Vector3 m_MoveInput;

        public void Init(Rigidbody2D rb, float speed)
        {
            m_Rigidbody = rb;
            m_Speed     = speed;
            m_This      = this;
        }

        void IMovementDriver.SetMoveInput(Vector3 worldMove)
        {
            m_MoveInput = worldMove;
        }

        void IMovementDriver.Tick(float deltaTime)
        {
            if (m_MoveInput.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Vector2 velocity = new Vector2(m_MoveInput.x, m_MoveInput.y).normalized * m_Speed;
            m_Rigidbody.MovePosition(m_Rigidbody.position + velocity * deltaTime);
        }

        void IMovementDriver.SetPosition(Vector3 position)
        {
            m_Rigidbody.MovePosition(position);
        }

        void IMovementDriver.SetRotation(Quaternion rotation)
        {
            m_Rigidbody.MoveRotation(rotation);
        }

        async Task IMovementDriver.SetRotationAsync(SetEntityRotationAsyncArgs args)
        {
            float start   = m_Rigidbody.rotation;
            float targetZ = args.Rotation.eulerAngles.z;

            float elapsed = 0f;

            while (elapsed < args.Duration)
            {
                await Awaitable.FixedUpdateAsync();

                elapsed += Time.fixedDeltaTime;

                float t = math.clamp(elapsed / args.Duration, 0f, 1f);
                t = RotationUtility.ApplyInterpolation(t, args.RotationType);

                float rot = MathUtils.LerpAngle(start, targetZ, t);

                m_Rigidbody.MoveRotation(rot);
            }

            m_Rigidbody.MoveRotation(targetZ);
        }

        private void FixedUpdate()
        {
            m_This.Tick(Time.fixedDeltaTime);
        }
    }
}