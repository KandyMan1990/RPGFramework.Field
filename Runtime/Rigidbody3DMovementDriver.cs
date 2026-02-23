using System.Threading.Tasks;
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

        private Vector3 m_MoveInput;

        public void Init(Rigidbody rb, float speed)
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

            Vector3 direction = m_MoveInput.normalized;
            Vector3 velocity  = direction * m_Speed;

            m_Rigidbody.MovePosition(m_Rigidbody.position + velocity * deltaTime);
            m_Rigidbody.transform.forward = direction;
        }

        void IMovementDriver.SetRotation(Quaternion rotation)
        {
            m_Rigidbody.MoveRotation(rotation);
        }

        async Task IMovementDriver.SetRotationAsync(SetEntityRotationAsyncArgs args)
        {
            Quaternion start  = m_Rigidbody.rotation;
            Quaternion target = RotationUtility.AdjustDirection(start, args.Rotation, args.RotationDirection);

            float elapsed = 0f;

            while (elapsed < args.Duration)
            {
                await Awaitable.FixedUpdateAsync();

                elapsed += Time.fixedDeltaTime;

                float t = math.clamp(elapsed / args.Duration, 0f, 1f);
                t = RotationUtility.ApplyInterpolation(t, args.RotationType);

                Quaternion rot = Quaternion.Slerp(start, target, t);

                m_Rigidbody.MoveRotation(rot);
            }

            m_Rigidbody.MoveRotation(target);
        }

        private void FixedUpdate()
        {
            m_This.Tick(Time.fixedDeltaTime);
        }
    }
}