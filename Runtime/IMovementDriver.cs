using RPGFramework.Field.FieldVmArgs;
using UnityEngine;

namespace RPGFramework.Field
{
    internal interface IMovementDriver
    {
        void          SetMoveInput(Vector3                     worldMove);
        void          SetMoveSpeed(float                       speed);
        void          Tick(float                               deltaTime);
        void          SetPosition(Vector3                      position);
        void          SetRotation(Quaternion                   rotation);
        void          StartRotation(SetEntityRotationAsyncArgs args);
        void          ResumeRotation(RotationState             rotationState);
        RotationState GetRotationState();
    }
}