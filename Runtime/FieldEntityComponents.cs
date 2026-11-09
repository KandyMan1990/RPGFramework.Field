using RPGFramework.Field.FieldVmArgs;

namespace RPGFramework.Field
{
    internal sealed class FieldEntityComponents
    {
        internal FieldEntity             Entity             { get; private set; }
        internal IMovementDriver         MovementDriver     { get; private set; }
        internal IAnimationDriver        AnimationDriver    { get; private set; }
        internal FieldCollisionTrigger   CollisionTrigger   { get; private set; }
        internal FieldInteractionTrigger InteractionTrigger { get; private set; }
        internal MoveEntityArgs          Move               { get; private set; }
        internal bool                    HasMove            { get; private set; }
        internal bool                    IsLeftOut          { get; private set; }
        internal bool                    HasWalkCycle       { get; private set; }
        internal float                   WalkCycle          { get; private set; }

        private int m_StepsTaken;

        internal void SetEntity(FieldEntity                         entity)             => Entity = entity;
        internal void SetMovementDriver(IMovementDriver             movementDriver)     => MovementDriver = movementDriver;
        internal void SetAnimationDriver(IAnimationDriver           animationDriver)    => AnimationDriver = animationDriver;
        internal void SetCollisionTrigger(FieldCollisionTrigger     collisionTrigger)   => CollisionTrigger = collisionTrigger;
        internal void SetInteractionTrigger(FieldInteractionTrigger interactionTrigger) => InteractionTrigger = interactionTrigger;

        internal void SetMove(MoveEntityArgs move)
        {
            Move    = move;
            HasMove = true;
        }

        internal void ClearMove() => HasMove = false;

        internal void LeaveOut() => IsLeftOut = true;

        internal void SetWalkCycle(float cycle)
        {
            WalkCycle    = cycle;
            HasWalkCycle = true;
        }

        /// <summary>Stopped walking, so the next step is the first of its sound's order again.</summary>
        internal void ClearWalkCycle()
        {
            HasWalkCycle = false;
            m_StepsTaken = 0;
        }

        /// <returns>How many steps it had taken since it last stopped.</returns>
        internal int TakeStep() => m_StepsTaken++;
    }
}