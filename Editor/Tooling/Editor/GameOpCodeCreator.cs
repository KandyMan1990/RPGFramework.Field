using UnityEditor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace RPGFramework.Field.Editor
{
    /// <summary>
    /// Assets › Create › RPG Framework › Field › Game Opcodes: a script holding the game's own opcode enum, its first value
    /// <see cref="FieldScriptOpCodeRanges.GAME_BASE" />, and the <see cref="IFieldGameOpCodeDatabase" /> that runs them,
    /// in one file so it compiles at once; they can be split once it grows.
    /// </summary>
    internal static class GameOpCodeCreator
    {
        private const string TEMPLATE = "Editor/Templates/GameOpCode.cs.txt";

        [MenuItem("Assets/Create/RPG Framework/Field/Game Opcodes", priority = 0)]
        private static void Create()
        {
            PackageInfo package = PackageInfo.FindForAssembly(typeof(GameOpCodeCreator).Assembly);

            ProjectWindowUtil.CreateScriptAssetFromTemplateFile($"{package.resolvedPath}/{TEMPLATE}", "GameOpCode.cs");
        }
    }
}
