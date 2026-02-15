using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPGFramework.Field
{
    public sealed class FieldInteractionTrigger : MonoBehaviour
    {
        private const string PLAYER_TAG = "Player";

        public event Action<int, int> OnInteracted;
        public event Action<int>      OnTriggerEntered;
        public event Action<int>      OnTriggerExited;

        public float InteractionAngle => m_InteractionAngle;

        // TODO: show this in the editor via gizmo
        [SerializeField]
        private float m_InteractionAngle = 60f;

        private Collider    m_Collider;
        private FieldEntity m_Entity;
        private bool        m_IsActive;
        private int         m_EntityId;

        private static readonly Dictionary<Type, Func<Component, Action<float>>> m_ResizeStrategies
                = new Dictionary<Type, Func<Component, Action<float>>>
                  {
                          {
                                  typeof(SphereCollider), c =>
                                                          {
                                                              SphereCollider col = (SphereCollider)c;
                                                              return size => col.radius = size;
                                                          }
                          },
                          {
                                  typeof(CapsuleCollider), c =>
                                                           {
                                                               CapsuleCollider col = (CapsuleCollider)c;
                                                               return size => col.radius = size;
                                                           }
                          },
                          {
                                  typeof(BoxCollider), c =>
                                                       {
                                                           BoxCollider col = (BoxCollider)c;
                                                           return size => col.size = Vector3.one * size;
                                                       }
                          },
                          {
                                  typeof(CircleCollider2D), c =>
                                                            {
                                                                CircleCollider2D col = (CircleCollider2D)c;
                                                                return size => col.radius = size;
                                                            }
                          },
                          {
                                  typeof(CapsuleCollider2D), c =>
                                                             {
                                                                 CapsuleCollider2D col = (CapsuleCollider2D)c;
                                                                 return size => col.size = Vector2.one * size;
                                                             }
                          },
                          {
                                  typeof(BoxCollider2D), c =>
                                                         {
                                                             BoxCollider2D col = (BoxCollider2D)c;
                                                             return size => col.size = Vector2.one * size;
                                                         }
                          }
                  };

        private Action<float> m_ResizeAction;

        private void Awake()
        {
            m_Entity   = GetComponentInParent<FieldEntity>();
            m_IsActive = true;
            m_EntityId = m_Entity.EntityId;

            Component interactionCollider = GetComponent<Collider>() as Component ?? GetComponent<Collider2D>();

            if (interactionCollider == null)
            {
                throw new MissingComponentException($"{nameof(FieldInteractionTrigger)}::{nameof(Awake)} No collider found for {gameObject.name}.");
            }

            Type type = interactionCollider.GetType();

            if (m_ResizeStrategies.TryGetValue(type, out Func<Component, Action<float>> factory))
            {
                m_ResizeAction = factory(interactionCollider);
            }
            else
            {
                throw new MissingComponentException($"{nameof(FieldInteractionTrigger)}::{nameof(Awake)} No supported collider found for {gameObject.name}.");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TriggerEnterLogic(other);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TriggerEnterLogic(other);
        }

        private void OnTriggerExit(Collider other)
        {
            TriggerExitLogic(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            TriggerExitLogic(other);
        }

        private void TriggerEnterLogic(Component other)
        {
            if (!m_IsActive)
            {
                return;
            }

            // TODO: don't want to rely on tag, need to know player entity ID
            if (!other.CompareTag(PLAYER_TAG))
            {
                return;
            }

            OnTriggerEntered?.Invoke(m_EntityId);
        }

        private void TriggerExitLogic(Component other)
        {
            if (!m_IsActive)
            {
                return;
            }

            // TODO: don't want to rely on tag, need to know player entity ID
            if (!other.CompareTag(PLAYER_TAG))
            {
                return;
            }

            OnTriggerExited?.Invoke(m_EntityId);
        }

        public void TryInteract()
        {
            if (!m_IsActive)
            {
                return;
            }

            FieldCompiledScript compiledScript = m_Entity.ScriptDefinition.GetScript(FieldScriptType.OnInteraction);

            OnInteracted?.Invoke(m_EntityId, compiledScript.ScriptId);
        }

        public void SetActive(bool active)
        {
            m_IsActive = active;
        }

        public void SetInteractionRange(float range)
        {
            m_ResizeAction(range);
        }
    }
}