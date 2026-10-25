using RPGFramework.DI;

namespace RPGFramework.Field.Sample2D
{
    /// <summary>The game's bindings for the field's scene: the database the Field Designer's export writes.</summary>
    internal sealed class FieldModuleSceneInstaller : SceneInstallerBase
    {
        public override void InstallBindings(IDIContainer container)
        {
            container.BindSingleton<IFieldDatabase, FieldDatabase>();
        }
    }
}
