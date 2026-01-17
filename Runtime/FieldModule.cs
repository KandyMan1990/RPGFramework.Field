using System.Threading.Tasks;
using RPGFramework.Core.Input;
using RPGFramework.Core.SharedTypes;
using RPGFramework.DI;
using RPGFramework.Field.SharedTypes;
using UnityEngine;

namespace RPGFramework.Field
{
    public class FieldModule : IFieldModule
    {
        private readonly IDIResolver m_DIResolver;

        private InputAdapter m_InputAdapter;

        public FieldModule(IDIResolver diResolver)
        {
            m_DIResolver = diResolver;
        }

        Task IModule.OnEnterAsync(IModuleArgs args)
        {
            m_InputAdapter = Object.FindFirstObjectByType<InputAdapter>();
            m_DIResolver.InjectInto(m_InputAdapter);
            m_InputAdapter.Enable();

            return Task.CompletedTask;
        }

        Task IModule.OnExitAsync()
        {
            m_InputAdapter.Disable();

            return Task.CompletedTask;
        }
    }
}