using System.Threading.Tasks;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// How a field's scenery and entities get into the scene: the game's choice, bound in the field's installer.
    /// </summary>
    public interface IFieldPresentation
    {
        /// <summary>Loads the field and puts it in the scene, returning its root.</summary>
        Task<GameObject> LoadAsync(FieldDefinition asset);

        /// <summary>Takes the field loaded last out of the scene and lets go of what it loaded.</summary>
        Task Unload();
    }
}