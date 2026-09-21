using System;
using RPGFramework.Localisation.Editor;
using UnityEngine;

namespace RPGFramework.Field.Editor
{
    [Serializable]
    internal class FieldDatabaseAssetAuthoring
    {
        public GameObject               Prefab;
        public LocalisationSheetAsset[] LocalisationSheets;
    }
}