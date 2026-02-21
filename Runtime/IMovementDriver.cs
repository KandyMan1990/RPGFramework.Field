using UnityEngine;

namespace RPGFramework.Field
{
    public interface IMovementDriver
    {
        void SetMoveInput(Vector3 worldMove);
        void Tick(float           deltaTime);
    }
}