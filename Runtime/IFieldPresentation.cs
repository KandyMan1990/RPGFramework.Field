using System.Threading.Tasks;
using UnityEngine;

namespace RPGFramework.Field
{
    public interface IFieldPresentation
    {
        Task<GameObject> LoadAsync(FieldDatabaseAsset asset);
        Task             Unload();
    }
}