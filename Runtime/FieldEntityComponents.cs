namespace RPGFramework.Field
{
    internal sealed class FieldEntityComponents
    {
        internal FieldEntity             Entity             { get; private set; }
        internal IMovementDriver         MovementDriver     { get; private set; }
        internal FieldGatewayTrigger     GatewayTrigger     { get; private set; }
        internal FieldInteractionTrigger InteractionTrigger { get; private set; }

        internal void SetEntity(FieldEntity                         entity)             => Entity = entity;
        internal void SetMovementDriver(IMovementDriver             movementDriver)     => MovementDriver = movementDriver;
        internal void SetGatewayTrigger(FieldGatewayTrigger         gatewayTrigger)     => GatewayTrigger = gatewayTrigger;
        internal void SetInteractionTrigger(FieldInteractionTrigger interactionTrigger) => InteractionTrigger = interactionTrigger;
    }
}