namespace RPGFramework.Field
{
    internal sealed class FieldEntityComponents
    {
        internal FieldEntity             Entity             { get; private set; }
        internal IMovementDriver         MovementDriver     { get; private set; }
        internal IAnimationDriver        AnimationDriver    { get; private set; }
        internal FieldCollisionTrigger   CollisionTrigger   { get; private set; }
        internal FieldInteractionTrigger InteractionTrigger { get; private set; }

        internal void SetEntity(FieldEntity                         entity)             => Entity = entity;
        internal void SetMovementDriver(IMovementDriver             movementDriver)     => MovementDriver = movementDriver;
        internal void SetAnimationDriver(IAnimationDriver           animationDriver)    => AnimationDriver = animationDriver;
        internal void SetCollisionTrigger(FieldCollisionTrigger     collisionTrigger)   => CollisionTrigger = collisionTrigger;
        internal void SetInteractionTrigger(FieldInteractionTrigger interactionTrigger) => InteractionTrigger = interactionTrigger;
    }
}