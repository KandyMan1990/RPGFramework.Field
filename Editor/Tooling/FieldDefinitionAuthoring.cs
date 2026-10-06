using System;
using RPGFramework.Localisation.Editor;
using UnityEngine;

namespace RPGFramework.Field.Editor
{
    [Serializable]
    internal class FieldDefinitionAuthoring
    {
        public GameObject               Prefab;
        public LocalisationSheetAsset[] LocalisationSheets;
        public string                   LocationName;
    }
}