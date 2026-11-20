using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace RPGFramework.Field.Editor
{
    /// <summary>
    /// Refuses to build a player whose fields were exported against another layout of the variable map, or another table
    /// of opcodes. Their scripts address variables by the offsets they had then, so after a variable has moved, or a new
    /// one has filled a gap a deleted one left, they would read and write the wrong bytes; and they hold the opcodes'
    /// values and their arguments as they were written, so after one has moved, the framework's or the game's, they
    /// would run the wrong ones, and after its arguments have changed, read them from the wrong bytes.
    /// </summary>
    internal sealed class FieldBuildCheck : IPreprocessBuildWithReport
    {
        int IOrderedCallback.callbackOrder => 0;

        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
        {
            FieldDesignerData data = FieldDesignerDataUtility.FindData();

            if (data == null || data.Fields.Count == 0)
            {
                return;
            }

            if (data.FieldDatabase.ExportedLayoutHash != FieldExport.LayoutHash(FieldScriptCompiler.LoadVariableMap()))
            {
                throw new BuildFailedException("The variable map has changed since the fields were last exported, and their scripts address variables where the map used to put them. Export the fields again from the Field Designer");
            }

            if (data.FieldDatabase.ExportedOpCodeHash != FieldExport.OpCodeHash(FieldOpCodeCatalogue.Project))
            {
                throw new BuildFailedException("The field opcodes, the framework's or the game's, or how their arguments are written, have changed since the fields were last exported, and their scripts hold the values and arguments they had then. Export the fields again from the Field Designer");
            }
        }
    }
}
