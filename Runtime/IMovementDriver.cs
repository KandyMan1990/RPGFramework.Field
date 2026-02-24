using System.Threading.Tasks;
using RPGFramework.Field.FieldVmArgs;
using UnityEngine;

namespace RPGFramework.Field
{
    internal interface IMovementDriver
    {
        void SetMoveInput(Vector3                        worldMove);
        void Tick(float                                  deltaTime);
        void SetPosition(Vector3                         position);
        void SetRotation(Quaternion                      rotation);
        Task SetRotationAsync(SetEntityRotationAsyncArgs args);
    }
}