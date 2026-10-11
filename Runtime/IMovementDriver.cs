using System.Collections.Generic;
using RPGFramework.Field.FieldVmArgs;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// How an entity moves — a transform, a 2D or 3D rigidbody, a tilemap's cells — so each shape of game holds its
    /// characters to the ground its own way. The field ticks it, and feeds it the player's input or a script's moves.
    /// </summary>
    internal interface IMovementDriver
    {
        /// <summary>The direction to move in, in world space, as the player's input gives it; zero to stand.</summary>
        void SetMoveInput(Vector3 worldMove);

        /// <summary>How fast the entity moves, in units a second.</summary>
        void SetMoveSpeed(float speed);

        /// <summary>A frame's movement and turning, for a driver that moves per frame.</summary>
        void Tick(float deltaTime);

        /// <summary>A physics step's movement and turning, for a driver that moves with physics.</summary>
        void PhysicsTick(float fixedDeltaTime);

        /// <summary>Puts the entity at a position at once.</summary>
        void SetPosition(Vector3 position);

        /// <summary>Turns the entity to a rotation at once.</summary>
        void SetRotation(Quaternion rotation);

        /// <summary>Starts a turn over time, as a script asks.</summary>
        void StartRotation(SetEntityRotationAsyncArgs args);

        /// <summary>
        /// Carries on a turn from where <see cref="GetRotationState" /> left it, as a resumed field does.
        /// </summary>
        void ResumeRotation(RotationState rotationState);

        /// <summary>Where a turn under way has got to, for the field to keep and resume.</summary>
        RotationState GetRotationState();

        /// <summary>The velocity the entity is moving at, which its animation follows.</summary>
        Vector3 CurrentVelocity { get; }

        /// <summary>
        /// Walks to a point, stopping <paramref name="stopDistance" /> short of it, facing the way it travels when
        /// <paramref name="faceTravel" />.
        /// </summary>
        void MoveTo(Vector3 target, float stopDistance, bool faceTravel);

        /// <summary>Stops a <see cref="MoveTo" /> where it is.</summary>
        void StopMove();

        /// <summary>Whether a <see cref="MoveTo" /> is under way.</summary>
        bool IsMovingToTarget { get; }

        /// <summary>The entities this one walked into in its last physics step, for their push scripts.</summary>
        IReadOnlyList<FieldEntity> Pushed { get; }
    }
}