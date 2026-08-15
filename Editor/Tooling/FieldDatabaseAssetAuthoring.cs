using System;
using RPGFramework.Localisation.Editor;
using UnityEngine;

namespace RPGFramework.Field.Editor
{
    [Serializable]
    public class FieldDatabaseAssetAuthoring
    {
        public GameObject               Prefab;
        public LocalisationSheetAsset[] LocalisationSheets;
    }
}