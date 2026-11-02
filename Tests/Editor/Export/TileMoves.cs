using System.Collections.Generic;
using RPGFramework.Field.Editor;
using RPGFramework.Field.Tests.Movement;
using UnityEngine;

namespace RPGFramework.Field.Tests.Export
{
    /// <summary>
    /// A tile field of five by five tiles, cells -2 to 2 each way, so centres at -1.5 to 2.5, and export's check of
    /// the points a script's moves go to on it.
    /// </summary>
    internal static class TileMoves
    {
        internal static List<string> Problems(params string[] lines)
        {
            TileField field = new TileField(Vector3Int.zero);

            try
            {
                List<string> problems = new List<string>();

                FieldDatabase.ValidateMoveTargets(field.Tilemap, "script", lines, problems);

                return problems;
            }
            finally
            {
                field.Destroy();
            }
        }
    }
}
