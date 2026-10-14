using System;
using RPGFramework.Field.FieldVmArgs;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>Which of an entity's values a script has set, or the field took as it was left.</summary>
    [Flags]
    internal enum FieldEntityValues : ushort
    {
        None                   = 0,
        Pose                   = 1 << 0,
        RotationState          = 1 << 1,
        Visible                = 1 << 2,
        InteractionActive      = 1 << 3,
        Solid                  = 1 << 4,
        PushActive             = 1 << 5,
        InteractionRange       = 1 << 6,
        MovementSpeed          = 1 << 7,
        Move                   = 1 << 8,
        BaseAnimation          = 1 << 9,
        AnimationSpeed         = 1 << 10,
        PlayedAnimation        = 1 << 11,
        CollisionTriggerActive = 1 << 12
    }

    /// <summary>
    /// One entity's state that outlives the field module, in plain values: what its scripts set, written here once as
    /// they set it, and where it stood and what it was doing as the field was left. Only values in <see cref="Set" />
    /// are put back on a return, since an entity no script touched keeps what its prefab gives it — hiding one also
    /// switches its interaction off, so replaying an untouched entity's visibility would change it.
    /// </summary>
    internal struct FieldEntityState
    {
        internal FieldEntityValues Set;
        internal Vector3           Position;
        internal Quaternion        Rotation;
        internal RotationState     RotationState;
        internal bool              Visible;
        internal bool              InteractionActive;
        internal bool              Solid;
        internal bool              PushActive;
        internal bool              CollisionTriggerActive;
        internal float             InteractionRange;
        internal float             MovementSpeed;
        internal float             AnimationSpeed;
        internal ulong             BaseAnimation;
        internal PlayAnimationArgs PlayedAnimation;
        internal MoveEntityArgs    Move;

        internal void SetPose(Vector3 position, Quaternion rotation)
        {
            Position =  position;
            Rotation =  rotation;
            Set      |= FieldEntityValues.Pose;
        }

        internal void SetRotationState(RotationState rotationState)
        {
            RotationState =  rotationState;
            Set           |= FieldEntityValues.RotationState;
        }

        internal void SetVisible(bool visible)
        {
            Visible =  visible;
            Set     |= FieldEntityValues.Visible;
        }

        internal void SetInteractionActive(bool active)
        {
            InteractionActive =  active;
            Set               |= FieldEntityValues.InteractionActive;
        }

        internal void SetSolid(bool solid)
        {
            Solid =  solid;
            Set   |= FieldEntityValues.Solid;
        }

        internal void SetPushActive(bool active)
        {
            PushActive =  active;
            Set        |= FieldEntityValues.PushActive;
        }

        internal void SetCollisionTriggerActive(bool active)
        {
            CollisionTriggerActive =  active;
            Set                    |= FieldEntityValues.CollisionTriggerActive;
        }

        internal void SetInteractionRange(float range)
        {
            InteractionRange =  range;
            Set              |= FieldEntityValues.InteractionRange;
        }

        internal void SetMovementSpeed(float speed)
        {
            MovementSpeed =  speed;
            Set           |= FieldEntityValues.MovementSpeed;
        }

        internal void SetAnimationSpeed(float multiplier)
        {
            AnimationSpeed =  multiplier;
            Set            |= FieldEntityValues.AnimationSpeed;
        }

        internal void SetBaseAnimation(ulong stateNameHash)
        {
            BaseAnimation =  stateNameHash;
            Set           |= FieldEntityValues.BaseAnimation;
        }

        /// <summary>
        /// A looping or held animation is still showing when the field is left, so it is put back on return. One that
        /// returns to base is not: it would replay from the start, which is not where the player left it.
        /// </summary>
        internal void SetPlayedAnimation(PlayAnimationArgs args)
        {
            if (args.Mode == AnimationPlayMode.ReturnToBase)
            {
                ClearPlayedAnimation();

                return;
            }

            PlayedAnimation =  args;
            Set             |= FieldEntityValues.PlayedAnimation;
        }

        internal void ClearPlayedAnimation() => Set &= ~FieldEntityValues.PlayedAnimation;

        internal void SetMove(MoveEntityArgs move)
        {
            Move =  move;
            Set  |= FieldEntityValues.Move;
        }

        internal void ClearMove() => Set &= ~FieldEntityValues.Move;
    }

    /// <summary>A blocker's state that outlives the field module, once a script has switched it.</summary>
    internal struct FieldBlockerState
    {
        internal byte BlockerId;
        internal bool Set;
        internal bool Active;

        internal void SetActive(bool active)
        {
            Active = active;
            Set    = true;
        }
    }
}