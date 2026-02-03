using System.Threading.Tasks;
using UnityEngine;

namespace RPGFramework.Field
{
    public sealed class PrefabFieldPresentation : IFieldPresentation
    {
        private GameObject m_Instance;

        async Task<GameObject> IFieldPresentation.LoadAsync(FieldDefinition field)
        {
            GameObject prefab = Resources.Load<GameObject>(field.PrefabAddress);

            GameObject[] op = await Object.InstantiateAsync(prefab);
            m_Instance = op[0];

            return m_Instance;
        }

        void IFieldPresentation.Unload()
        {
            Object.Destroy(m_Instance);
        }
    }
}