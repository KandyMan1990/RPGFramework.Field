using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace RPGFramework.Field.Editor
{
    /// <summary>
    /// Refuses to build a player whose fields were exported against another layout of the variable map. Their scripts
    /// address variables by the offsets they had then, so after a variable has moved, or a new one has filled a gap a
    /// deleted one left, they would read and write the wrong bytes.
    /// </summary>
    internal sealed class FieldBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
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
        }
    }
}
