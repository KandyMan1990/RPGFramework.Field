using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGFramework.Audio;
using RPGFramework.Core;
using RPGFramework.Core.Input;
using RPGFramework.Core.PlayerLoop;
using RPGFramework.Core.SharedTypes;
using RPGFramework.DI;
using RPGFramework.Field.SharedTypes;
using RPGFramework.Menu.SharedTypes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGFramework.Field
{
    public class FieldModule : IFieldModule, IInputContext, IUpdatable
    {
        private readonly ICoreModule        m_CoreModule;
        private readonly IDIResolver        m_DIResolver;
        private readonly IInputRouter       m_InputRouter;
        private readonly IMenuTypeProvider  m_MenuTypeProvider;
        private readonly IMusicPlayer       m_MusicPlayer;
        private readonly ISfxPlayer         m_SfxPlayer;
        private readonly IFieldRegistry     m_FieldRegistry;
        private readonly IFieldPresentation m_FieldPresentation;

        private InputAdapter                             m_InputAdapter;
        private FieldContext                             m_FieldContext;
        private SpawnPoint                               m_SpawnPoint;
        private Dictionary<int, FieldEntity>             m_EntityGameObjects;
        private Dictionary<int, FieldGatewayTrigger>     m_EntityGatewayTriggers;
        private Dictionary<int, FieldInteractionTrigger> m_EntityInteractionTriggers;
        private int                                      m_CurrentInteractionTriggerId;

        private IFieldModuleArgs m_FieldTransitionArgs;
        private bool             m_FieldTransitionRequested = false;

        public FieldModule(ICoreModule        coreModule,
                           IDIResolver        diResolver,
                           IInputRouter       inputRouter,
                           IMenuTypeProvider  menuTypeProvider,
                           IMusicPlayer       musicPlayer,
                           ISfxPlayer         sfxPlayer,
                           IFieldRegistry     fieldRegistry,
                           IFieldPresentation fieldPresentation)
        {
            m_CoreModule        = coreModule;
            m_DIResolver        = diResolver;
            m_InputRouter       = inputRouter;
            m_MenuTypeProvider  = menuTypeProvider;
            m_MusicPlayer       = musicPlayer;
            m_SfxPlayer         = sfxPlayer;
            m_FieldRegistry     = fieldRegistry;
            m_FieldPresentation = fieldPresentation;
        }

        async Task IModule.OnEnterAsync(IModuleArgs args)
        {
            m_InputAdapter = Object.FindFirstObjectByType<InputAdapter>();
            m_DIResolver.InjectInto(m_InputAdapter);

            IFieldModuleArgs fieldArgs = (IFieldModuleArgs)args;

            await LoadNewFieldAsync(fieldArgs);
        }

        async Task IModule.OnExitAsync()
        {
            await UnloadCurrentFieldAsync();

            m_CoreModule.ResetModule<IFieldModule, FieldModule>();
        }

        Task IFieldModule.LoadMenuModuleAsync(byte menuId)
        {
            Type            type = m_MenuTypeProvider.GetType(menuId);
            IMenuModuleArgs args = new MenuModuleArgs(type);

            return m_CoreModule.LoadModuleAsync<IMenuModule>(args);
        }

        bool IInputContext.Handle(ControlSlot slot)
        {
            switch (slot)
            {
                case ControlSlot.Primary:
                    if (m_CurrentInteractionTriggerId > -1 && CanInteract(m_CurrentInteractionTriggerId))
                    {
                        m_EntityInteractionTriggers[m_CurrentInteractionTriggerId].TryInteract();
                    }
                    break;
                case ControlSlot.Tertiary:
                {
                    Type            type = m_MenuTypeProvider.GetType(MenuType.Config);
                    IMenuModuleArgs args = new MenuModuleArgs(type);

                    m_CoreModule.LoadModuleAsync<IMenuModule>(args).FireAndForget();
                    break;
                }
            }

            return true;
        }

        void IUpdatable.Update()
        {
            foreach (FieldEntityRuntime entity in m_FieldContext.Entities)
            {
                entity.Update(m_FieldContext.VM);
            }

            if (m_FieldTransitionRequested)
            {
                TriggerFieldTransitionAsync().FireAndForget();
            }
        }

        private void SetFieldModuleArgs(IFieldModuleArgs args)
        {
            m_FieldTransitionArgs      = args;
            m_FieldTransitionRequested = true;
        }

        private async Task TriggerFieldTransitionAsync()
        {
            m_FieldTransitionRequested = false;

            await UnloadCurrentFieldAsync();
            await LoadNewFieldAsync(m_FieldTransitionArgs);
        }

        private async Task LoadNewFieldAsync(IFieldModuleArgs args)
        {
            FieldDefinition fieldDefinition = m_FieldRegistry.LoadField(args.GetFieldId);

            GameObject   fieldGameObject = await m_FieldPresentation.LoadAsync(fieldDefinition);
            SpawnPoint[] spawnPoints     = fieldGameObject.GetComponentsInChildren<SpawnPoint>();

            m_SpawnPoint = Array.Find(spawnPoints, sp => sp.Id == args.GetSpawnId);

            FieldVM vm = new FieldVM();

            FieldEntity[] entitiesInGameObject = fieldGameObject.GetComponentsInChildren<FieldEntity>();

            m_EntityGameObjects         = new Dictionary<int, FieldEntity>(entitiesInGameObject.Length);
            m_EntityGatewayTriggers     = new Dictionary<int, FieldGatewayTrigger>();
            m_EntityInteractionTriggers = new Dictionary<int, FieldInteractionTrigger>();

            List<FieldEntityRuntime> entities = new List<FieldEntityRuntime>(entitiesInGameObject.Length);

            int scriptId = 0;

            foreach (FieldEntity entity in entitiesInGameObject)
            {
                m_EntityGameObjects.Add(entity.EntityId, entity);
                FieldGatewayTrigger gatewayTrigger = entity.GetComponentInChildren<FieldGatewayTrigger>();

                if (gatewayTrigger != null)
                {
                    m_EntityGatewayTriggers.Add(entity.EntityId, gatewayTrigger);
                    gatewayTrigger.OnTriggered += OnGatewayTriggered;
                }

                FieldInteractionTrigger interactionTrigger = entity.GetComponentInChildren<FieldInteractionTrigger>();

                if (interactionTrigger != null)
                {
                    m_EntityInteractionTriggers.Add(entity.EntityId, interactionTrigger);
                    interactionTrigger.OnInteracted     += OnInteractionTriggered;
                    interactionTrigger.OnTriggerEntered += OnInteractionTriggerEntered;
                    interactionTrigger.OnTriggerExited  += OnInteractionTriggerExited;
                }

                // TODO: ensure entity has a FieldScriptType.Init script as its first script
                FieldEntityRuntime fieldEntityRuntime = new FieldEntityRuntime(entity.EntityId, scriptId);

                entities.Add(fieldEntityRuntime);
                vm.RegisterEntity(entity.EntityId, fieldEntityRuntime);

                foreach (ScriptEntry scriptEntry in entity.ScriptDefinition.Scripts)
                {
                    vm.RegisterScript(scriptId, scriptEntry.CompiledScript);
                    scriptId++;
                }
            }

            m_FieldContext = new FieldContext(vm, entities);

            m_CurrentInteractionTriggerId = -1;

            vm.RequestFieldTransition          += SetFieldModuleArgs;
            vm.RequestMusic                    += RequestMusic;
            vm.RequestSfx                      += RequestSfx;
            vm.RequestSetPlayerEntity          += RequestSetPlayerEntity;
            vm.RequestSetEntityVisible         += RequestSetEntityVisible;
            vm.RequestSetGatewayTriggersActive += RequestSetGatewayTriggersActive;

            UpdateManager.RegisterUpdatable(this);

            m_InputRouter.Push(this);

            m_InputAdapter.Enable();
        }

        private Task UnloadCurrentFieldAsync()
        {
            m_InputAdapter.Disable();

            m_InputRouter.Pop(this);

            UpdateManager.QueueForUnregisterUpdatable(this);

            m_FieldContext.VM.RequestSetGatewayTriggersActive -= RequestSetGatewayTriggersActive;
            m_FieldContext.VM.RequestSetEntityVisible         -= RequestSetEntityVisible;
            m_FieldContext.VM.RequestSetPlayerEntity          -= RequestSetPlayerEntity;
            m_FieldContext.VM.RequestSfx                      -= RequestSfx;
            m_FieldContext.VM.RequestMusic                    -= RequestMusic;
            m_FieldContext.VM.RequestFieldTransition          -= SetFieldModuleArgs;

            foreach (KeyValuePair<int, FieldInteractionTrigger> entityInteractionTrigger in m_EntityInteractionTriggers)
            {
                entityInteractionTrigger.Value.OnTriggerExited  -= OnInteractionTriggerExited;
                entityInteractionTrigger.Value.OnTriggerEntered -= OnInteractionTriggerEntered;
                entityInteractionTrigger.Value.OnInteracted     -= OnInteractionTriggered;
            }

            foreach (KeyValuePair<int, FieldGatewayTrigger> entityGatewayTrigger in m_EntityGatewayTriggers)
            {
                entityGatewayTrigger.Value.OnTriggered -= OnGatewayTriggered;
            }

            m_FieldContext = null;

            m_FieldPresentation.Unload();

            return Task.CompletedTask;
        }

        private void RequestMusic(int id)
        {
            m_MusicPlayer.Play(id).FireAndForget();
        }

        private void RequestSfx(int id)
        {
            m_SfxPlayer.Play(id);
        }

        private void RequestSetPlayerEntity(FieldEntityRuntime entity)
        {
            m_FieldContext.SetPlayerEntity(entity);

            m_EntityGameObjects[entity.EntityId].transform.SetPositionAndRotation(m_SpawnPoint.Position, m_SpawnPoint.Rotation);
        }

        private void RequestSetEntityVisible(int entityId, bool visible)
        {
            m_EntityGameObjects[entityId].SetVisible(visible);
        }

        private void OnGatewayTriggered(int entityId, int scriptId)
        {
            m_FieldContext.VM.RequestScriptImmediately(entityId, scriptId);
        }

        private void OnInteractionTriggered(int entityId, int scriptId)
        {
            m_FieldContext.VM.RequestScriptImmediately(entityId, scriptId);
        }

        private void OnInteractionTriggerEntered(int entityId)
        {
            m_CurrentInteractionTriggerId = entityId;
        }

        private void OnInteractionTriggerExited(int entityId)
        {
            if (m_CurrentInteractionTriggerId == entityId)
            {
                m_CurrentInteractionTriggerId = -1;
            }
        }

        private bool CanInteract(int entityId)
        {
            // is player facing the m_CurrentInteractionTriggerId entity?
            return true;
        }

        private void RequestSetGatewayTriggersActive(bool active)
        {
            foreach (FieldGatewayTrigger fieldGatewayTrigger in m_EntityGatewayTriggers.Values)
            {
                fieldGatewayTrigger.SetActive(active);
            }
        }
    }
}