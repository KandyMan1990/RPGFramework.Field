using System.Threading.Tasks;
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

        private Vector3 m_MoveInput;

        public void Init(Transform entityTransform, float speed)
        {
            m_Transform = entityTransform;
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

            m_Transform.position += direction * (m_Speed * deltaTime);
            m_Transform.forward  =  direction;
        }

        void IMovementDriver.SetPosition(Vector3 position)
        {
            m_Transform.position = position;
        }

        void IMovementDriver.SetRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
        }

        async Task IMovementDriver.SetRotationAsync(SetEntityRotationAsyncArgs args)
        {
            Quaternion start  = m_Transform.rotation;
            Quaternion target = RotationUtility.AdjustDirection(start, args.Rotation, args.RotationDirection);

            float elapsed = 0f;

            while (elapsed < args.Duration)
            {
                elapsed += Time.deltaTime;

                float t = math.clamp(elapsed / args.Duration, 0f, 1f);
                t = RotationUtility.ApplyInterpolation(t, args.RotationType);

                m_Transform.rotation = Quaternion.Slerp(start, target, t);

                await Awaitable.NextFrameAsync();
            }

            m_Transform.rotation = target;
        }

        private void Update()
        {
            m_This.Tick(Time.deltaTime);
        }
    }
}