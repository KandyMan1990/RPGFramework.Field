using System.Threading.Tasks;
using UnityEngine;

namespace RPGFramework.Field
{
    public interface IFieldPresentation
    {
        Task<GameObject> LoadAsync(FieldDefinition field);
        void             Unload();
    }
}