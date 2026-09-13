#if UNITY_EDITOR
using UnityEngine;

namespace RPGFramework.Field
{
    public partial class FieldModule
    {
        private FieldDebugOverlay m_DebugOverlay;

        /// <summary>
        /// White: the player's facing cone. Circles: each interactable entity's range, green if active and grey if
        /// not, with its facing arc. Lines to entities in range: green for the one an interaction would pick,
        /// yellow when the player is not facing it, red when it is not facing the player.
        /// </summary>
        private void DrawInteractionDebug()
        {
            if (!FieldDebugDrawing.Interaction)
            {
                m_DebugOverlay?.RemoveFromHierarchy();
                m_DebugOverlay = null;

                return;
            }

            if (m_FieldContext.PlayerEntity == null)
            {
                return;
            }

            if (m_DebugOverlay == null)
            {
                m_DebugOverlay = new FieldDebugOverlay();
                m_RootElement.Add(m_DebugOverlay);
            }

            m_DebugOverlay.Begin(m_FieldModuleMonoBehaviour.Camera);

            Transform player    = m_Entities[m_FieldContext.PlayerEntity.EntityId].Entity.transform;
            Vector3   up        = m_FieldModuleMonoBehaviour.Up;
            Vector3   playerPos = player.position;
            Vector3   forward   = Vector3.ProjectOnPlane(player.forward, up).normalized;
            float     halfCone  = m_FieldModuleMonoBehaviour.PlayerInteractionAngle / 2f;

            m_DebugOverlay.Line(playerPos, playerPos + Quaternion.AngleAxis(-halfCone, up) * forward * 2f, Color.white);
            m_DebugOverlay.Line(playerPos, playerPos + Quaternion.AngleAxis(halfCone,  up) * forward * 2f, Color.white);

            FieldInteractionTrigger best = GetBestInteractionTrigger();

            foreach (FieldEntityComponents entity in m_Entities.Values)
            {
                FieldInteractionTrigger trigger = entity.InteractionTrigger;

                if (trigger == null)
                {
                    continue;
                }

                Transform entityTransform = entity.Entity.transform;
                Vector3   entityPos       = entityTransform.position;
                Vector3   entityForward   = Vector3.ProjectOnPlane(entityTransform.forward, up).normalized;
                Color     rangeColour     = trigger.IsActive ? Color.green : Color.gray;
                Vector3   arcStart        = Quaternion.AngleAxis(-trigger.InteractionAngle / 2f, up) * entityForward;

                m_DebugOverlay.Arc(entityPos, up, entityForward, 360f, trigger.InteractionRange, rangeColour);
                m_DebugOverlay.Line(entityPos, entityPos + arcStart                                           * trigger.InteractionRange,            rangeColour);
                m_DebugOverlay.Line(entityPos, entityPos + Quaternion.AngleAxis(trigger.InteractionAngle, up) * arcStart * trigger.InteractionRange, rangeColour);

                if (!trigger.IsActive || !IsInInteractionRange(playerPos, entity))
                {
                    continue;
                }

                int entityId = trigger.Entity.EntityId;

                Color outcome = trigger == best                   ? Color.green
                                : !IsPlayerFacingEntity(entityId) ? Color.yellow
                                                                    : Color.red;

                m_DebugOverlay.Line(playerPos, entityPos, outcome);
            }

            m_DebugOverlay.End();
        }
    }
}
#endif