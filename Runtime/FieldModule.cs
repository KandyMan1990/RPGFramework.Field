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

        private InputAdapter                 m_InputAdapter;
        private FieldContext                 m_FieldContext;
        private SpawnPoint                   m_SpawnPoint;
        private Dictionary<int, FieldEntity> m_EntityGameObjects;

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
            if (slot == ControlSlot.Tertiary)
            {
                Type            type = m_MenuTypeProvider.GetType(MenuType.Config);
                IMenuModuleArgs args = new MenuModuleArgs(type);

                m_CoreModule.LoadModuleAsync<IMenuModule>(args).FireAndForget();
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

            m_EntityGameObjects = new Dictionary<int, FieldEntity>(entitiesInGameObject.Length);

            List<FieldEntityRuntime> entities = new List<FieldEntityRuntime>(entitiesInGameObject.Length);

            int scriptId = 0;

            foreach (FieldEntity entity in entitiesInGameObject)
            {
                m_EntityGameObjects.Add(entity.EntityId, entity);
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

            vm.RequestFieldTransition  += SetFieldModuleArgs;
            vm.RequestMusic            += RequestMusic;
            vm.RequestSfx              += RequestSfx;
            vm.RequestSetPlayerEntity  += RequestSetPlayerEntity;
            vm.RequestSetEntityVisible += RequestSetEntityVisible;

            UpdateManager.RegisterUpdatable(this);

            m_InputRouter.Push(this);

            m_InputAdapter.Enable();
        }

        private Task UnloadCurrentFieldAsync()
        {
            m_InputAdapter.Disable();

            m_InputRouter.Pop(this);

            UpdateManager.QueueForUnregisterUpdatable(this);

            m_FieldContext.VM.RequestSetEntityVisible -= RequestSetEntityVisible;
            m_FieldContext.VM.RequestSetPlayerEntity  -= RequestSetPlayerEntity;
            m_FieldContext.VM.RequestSfx              -= RequestSfx;
            m_FieldContext.VM.RequestMusic            -= RequestMusic;
            m_FieldContext.VM.RequestFieldTransition  -= SetFieldModuleArgs;

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
    }
}