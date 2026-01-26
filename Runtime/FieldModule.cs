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
using Object = UnityEngine.Object;

namespace RPGFramework.Field
{
    public class FieldModule : IFieldModule, IInputContext, IUpdatable
    {
        private readonly ICoreModule       m_CoreModule;
        private readonly IDIResolver       m_DIResolver;
        private readonly IInputRouter      m_InputRouter;
        private readonly IMenuTypeProvider m_MenuTypeProvider;
        private readonly IMusicPlayer      m_MusicPlayer;
        private readonly ISfxPlayer        m_SfxPlayer;

        private InputAdapter m_InputAdapter;
        private FieldContext m_FieldContext;

        public FieldModule(ICoreModule       coreModule,
                           IDIResolver       diResolver,
                           IInputRouter      inputRouter,
                           IMenuTypeProvider menuTypeProvider,
                           IMusicPlayer      musicPlayer,
                           ISfxPlayer        sfxPlayer)
        {
            m_CoreModule       = coreModule;
            m_DIResolver       = diResolver;
            m_InputRouter      = inputRouter;
            m_MenuTypeProvider = menuTypeProvider;
            m_MusicPlayer      = musicPlayer;
            m_SfxPlayer        = sfxPlayer;
        }

        Task IModule.OnEnterAsync(IModuleArgs args)
        {
            m_InputAdapter = Object.FindFirstObjectByType<InputAdapter>();
            m_DIResolver.InjectInto(m_InputAdapter);
            m_InputAdapter.Enable();

            List<FieldEntityRuntime> entities = new List<FieldEntityRuntime>
                                                {
                                                        new FieldEntityRuntime(0, 0),
                                                        new FieldEntityRuntime(1, 1)
                                                };

            Dictionary<int, FieldEntityRuntime> entityMap = new Dictionary<int, FieldEntityRuntime>();

            for (int i = 0; i < entities.Count; i++)
            {
                entityMap.Add(i, entities[i]);
            }

            FieldVM vm = new FieldVM(entityMap);
            vm.SetCallbackHandlers(m_MusicPlayer.Play, m_SfxPlayer.Play);

            m_FieldContext = new FieldContext(vm, entities);

            UpdateManager.RegisterUpdatable(this);

            m_InputRouter.Push(this);

            return Task.CompletedTask;
        }

        Task IModule.OnExitAsync()
        {
            m_InputRouter.Pop(this);

            UpdateManager.UnregisterUpdatable(this);

            m_FieldContext = null;

            m_InputAdapter.Disable();

            m_CoreModule.ResetModule<IFieldModule, FieldModule>();

            return Task.CompletedTask;
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
        }
    }
}