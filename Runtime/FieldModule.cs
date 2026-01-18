using System;
using System.Threading.Tasks;
using RPGFramework.Core;
using RPGFramework.Core.Input;
using RPGFramework.Core.SharedTypes;
using RPGFramework.DI;
using RPGFramework.Field.SharedTypes;
using RPGFramework.Menu.SharedTypes;
using Object = UnityEngine.Object;

namespace RPGFramework.Field
{
    public class FieldModule : IFieldModule, IInputContext
    {
        private readonly ICoreModule       m_CoreModule;
        private readonly IDIResolver       m_DIResolver;
        private readonly IInputRouter      m_InputRouter;
        private readonly IMenuTypeProvider m_MenuTypeProvider;

        private InputAdapter m_InputAdapter;

        public FieldModule(ICoreModule       coreModule,
                           IDIResolver       diResolver,
                           IInputRouter      inputRouter,
                           IMenuTypeProvider menuTypeProvider)
        {
            m_CoreModule       = coreModule;
            m_DIResolver       = diResolver;
            m_InputRouter      = inputRouter;
            m_MenuTypeProvider = menuTypeProvider;
        }

        Task IModule.OnEnterAsync(IModuleArgs args)
        {
            m_InputAdapter = Object.FindFirstObjectByType<InputAdapter>();
            m_DIResolver.InjectInto(m_InputAdapter);
            m_InputAdapter.Enable();

            m_InputRouter.Push(this);

            return Task.CompletedTask;
        }

        Task IModule.OnExitAsync()
        {
            m_InputRouter.Pop(this);

            m_InputAdapter.Disable();

            m_CoreModule.ResetModule<IFieldModule, FieldModule>();

            return Task.CompletedTask;
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
    }
}