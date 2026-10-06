using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RPGFramework.Field.Editor
{
    /// <summary>
    /// What an entity is in the field, as a role rather than a list of components: what the engine does with it
    /// decides what it carries. Each is described in <see cref="FieldEntityBodyBuilder.Describe" />, which the Field
    /// Designer shows beside the choice.
    /// </summary>
    internal enum FieldBodyPreset
    {
        Gateway,
        Character,
        PlayerCharacter,
        InteractableObject,
        ExaminePoint,
        Area,
        SavePoint,
        Plain
    }

    /// <summary>
    /// Builds what a field is made of — an entity's body with the components the engine looks for and its visuals as
    /// a prefab kept as a prefab, the scripts its role needs, a blocker, a spawn point — for the Field Designer and for
    /// the GameObject menu alike. Assembling one by hand is how a piece gets missed, which the export checks then have
    /// to catch.
    /// </summary>
    internal static class FieldEntityBodyBuilder
    {
        internal const string NEW_SCRIPT_TEXT = "RETURN";

        private const string TRIGGER_OBJECT = "Trigger";
        private const string MENU           = "GameObject/RPG Framework/Field/";
        private const int    MENU_PRIORITY  = 10;

        private const string SAVE_POINT_2D_VISUALS = "Packages/com.rpgframework.field/Runtime/Prefabs/SavePoint2D.prefab";
        private const string SAVE_POINT_3D_VISUALS = "Packages/com.rpgframework.field/Runtime/Prefabs/SavePoint3D.prefab";

        internal static FieldEntity Build(GameObject fieldRoot, FieldDimension dimension, FieldBodyPreset preset, string name, GameObject visuals)
        {
            GameObject bodyObject = new GameObject(name);
            bodyObject.transform.SetParent(fieldRoot.transform, false);

            FieldEntity entity = bodyObject.AddComponent<FieldEntity>();

            if (visuals != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(visuals, bodyObject.transform);
                instance.transform.localPosition = Vector3.zero;

                entity.SetVisibleObject(instance);
            }

            switch (preset)
            {
                case FieldBodyPreset.Character:
                    AddMovingBody(bodyObject, dimension);
                    bodyObject.AddComponent<FieldInteractionTrigger>();
                    break;

                case FieldBodyPreset.PlayerCharacter:
                    AddMovingBody(bodyObject, dimension);
                    break;

                case FieldBodyPreset.InteractableObject:
                    AddSolid(bodyObject, dimension);
                    bodyObject.AddComponent<FieldInteractionTrigger>();
                    break;

                case FieldBodyPreset.ExaminePoint:
                    bodyObject.AddComponent<FieldInteractionTrigger>();
                    break;

                case FieldBodyPreset.Gateway:
                case FieldBodyPreset.Area:
                case FieldBodyPreset.SavePoint:
                    AddTrigger(bodyObject, dimension);
                    break;
            }

            return entity;
        }

        /// <summary>
        /// A blocker at the field's origin, closed, with the next free id and a solid box of the field's dimension to
        /// block with, to be placed and sized in the prefab. A tilemap field swaps the box for a tilemap of the cells it
        /// closes.
        /// </summary>
        internal static FieldBlocker BuildBlocker(GameObject fieldRoot, FieldDimension dimension)
        {
            byte id = FieldBlocker.NextId(fieldRoot.transform, null);

            GameObject blockerObject = new GameObject($"Blocker{id}");
            blockerObject.transform.SetParent(fieldRoot.transform, false);

            FieldBlocker blocker = blockerObject.AddComponent<FieldBlocker>();
            blocker.SetId(id);

            AddSolid(blockerObject, dimension);

            return blocker;
        }

        /// <summary>
        /// The visuals a body of this kind starts with, which the author can replace: a placeholder save point, since a
        /// save point is something the player has to be able to see. None for anything else.
        /// </summary>
        internal static GameObject DefaultVisuals(FieldBodyPreset preset, FieldDimension dimension)
        {
            if (preset != FieldBodyPreset.SavePoint)
            {
                return null;
            }

            GameObject visuals = AssetDatabase.LoadAssetAtPath<GameObject>(dimension == FieldDimension.TwoD ? SAVE_POINT_2D_VISUALS : SAVE_POINT_3D_VISUALS);

            return visuals;
        }

        internal static string Describe(FieldBodyPreset preset)
        {
            string description = preset switch
                                 {
                                     FieldBodyPreset.Gateway            => "Walked into, and its Gateway script takes the player out of the field.",
                                     FieldBodyPreset.Character          => "Seen, moved, and talked to.",
                                     FieldBodyPreset.PlayerCharacter    => "The one the player drives: seen and moved, but nothing talks to it.",
                                     FieldBodyPreset.InteractableObject => "Seen, talked to, and walked into rather than through: a chest, a sign, something dropped.",
                                     FieldBodyPreset.ExaminePoint       => "Talked to and nothing else: a poster, a window, a place worth examining that is part of the scenery.",
                                     FieldBodyPreset.Area               => "An area the player walks into and out of, for enter and leave scripts that are not a way out.",
                                     FieldBodyPreset.SavePoint          => "An area where the player can save: entering it allows saving and leaving it stops it. For a game that saves only at save points.",
                                     FieldBodyPreset.Plain              => "Somewhere to be, and nothing else. For a body that is wired by hand.",
                                     _                                  => string.Empty
                                 };

            return description;
        }

        /// <summary>
        /// Kinematic, because the drivers sweep a body along its path themselves and a dynamic one would be pushed
        /// around by everything it touched.
        /// </summary>
        private static void AddMovingBody(GameObject bodyObject, FieldDimension dimension)
        {
            if (dimension == FieldDimension.TwoD)
            {
                Rigidbody2D body2D = bodyObject.AddComponent<Rigidbody2D>();
                body2D.bodyType = RigidbodyType2D.Kinematic;

                CapsuleCollider2D capsule2D = bodyObject.AddComponent<CapsuleCollider2D>();
                capsule2D.size   = new Vector2(1f, 2f);
                capsule2D.offset = new Vector2(0f, 1f);

                return;
            }

            Rigidbody body = bodyObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity  = false;

            CapsuleCollider capsule = bodyObject.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.5f;
            capsule.center = new Vector3(0f, 1f, 0f);
        }

        /// <summary>
        /// Solid, with no rigidbody: it never moves itself, and the drivers sweep against a collider whether or not it
        /// has one.
        /// </summary>
        private static void AddSolid(GameObject bodyObject, FieldDimension dimension)
        {
            if (dimension == FieldDimension.TwoD)
            {
                bodyObject.AddComponent<BoxCollider2D>().size = Vector2.one;

                return;
            }

            bodyObject.AddComponent<BoxCollider>().size = Vector3.one;
        }

        private static void AddTrigger(GameObject bodyObject, FieldDimension dimension)
        {
            GameObject triggerObject = new GameObject(TRIGGER_OBJECT);
            triggerObject.transform.SetParent(bodyObject.transform, false);

            if (dimension == FieldDimension.TwoD)
            {
                BoxCollider2D box2D = triggerObject.AddComponent<BoxCollider2D>();
                box2D.isTrigger = true;
                box2D.size      = Vector2.one;
            }
            else
            {
                BoxCollider box = triggerObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size      = Vector3.one;
            }

            triggerObject.AddComponent<FieldCollisionTrigger>();
        }

        /// <summary>
        /// A new entity with the next free id and an init script that returns, named for what it is.
        /// </summary>
        internal static FieldEntityRecord CreateEntityRecord(FieldEntities field, string namePrefix)
        {
            int entityId = 0;

            List<FieldEntityRecord> records = field.Entities;

            for (int i = 0; i < records.Count; i++)
            {
                FieldEntityRecord record = records[i];

                if (record.EntityId >= entityId)
                {
                    entityId = record.EntityId + 1;
                }
            }

            FieldEntityRecord entity = new FieldEntityRecord(entityId, $"{namePrefix}{entityId}", null);
            entity.Scripts.Add(new FieldScriptRecord(FieldScriptType.Init, string.Empty, NEW_SCRIPT_TEXT));

            field.Entities.Add(entity);

            return entity;
        }

        /// <summary>
        /// The scripts a body of this kind cannot do without: a gateway's script is what takes the player out of the
        /// field, and a save point is only its enter and leave scripts. Written only where the entity has none of that
        /// type, so nothing an author wrote is touched.
        /// </summary>
        internal static void WritePresetScripts(FieldEntityRecord entity, FieldBodyPreset preset, bool hasVisuals)
        {
            if (preset == FieldBodyPreset.Gateway)
            {
                AddScriptIfMissing(entity, FieldScriptType.Gateway, NEW_SCRIPT_TEXT);
            }

            if (preset == FieldBodyPreset.SavePoint)
            {
                AddScriptIfMissing(entity, FieldScriptType.OnEnter, "SAVE_ACCESSIBILITY true\n"  + NEW_SCRIPT_TEXT);
                AddScriptIfMissing(entity, FieldScriptType.OnLeave, "SAVE_ACCESSIBILITY false\n" + NEW_SCRIPT_TEXT);
            }

            WriteInitScript(entity, preset, hasVisuals);
        }

        private static void AddScriptIfMissing(FieldEntityRecord entity, FieldScriptType type, string text)
        {
            if (!entity.Scripts.Exists(script => script.Type == type))
            {
                entity.Scripts.Add(new FieldScriptRecord(type, string.Empty, text));
            }
        }

        /// <summary>
        /// What a body of this kind cannot do without: the player's entity is the one that claims the player, and an
        /// entity starts hidden, so anything with visuals has to show itself. Only written into an init script still
        /// left as a new entity's, so nothing an author wrote is touched.
        /// </summary>
        private static void WriteInitScript(FieldEntityRecord entity, FieldBodyPreset preset, bool hasVisuals)
        {
            FieldScriptRecord init = entity.Scripts.Find(script => script.Type == FieldScriptType.Init);

            if (init == null || init.Text != NEW_SCRIPT_TEXT)
            {
                return;
            }

            List<string> lines = new List<string>();

            if (preset == FieldBodyPreset.PlayerCharacter)
            {
                lines.Add("SET_PLAYER_ENTITY");
            }

            if (hasVisuals)
            {
                lines.Add("VISIBILITY true");
            }

            if (lines.Count == 0)
            {
                return;
            }

            lines.Add(NEW_SCRIPT_TEXT);

            init.SetText(string.Join("\n", lines));
        }

        // The GameObject menu, for a designer already in the prefab: each item makes the whole thing — an entity is its
        // record, body and scripts at once — under the object right-clicked, or the field's root, ready to be placed.

        [MenuItem(MENU + "Entity/Gateway", false, MENU_PRIORITY)]
        private static void CreateGateway(MenuCommand command) => CreateEntity(command, FieldBodyPreset.Gateway);

        [MenuItem(MENU + "Entity/Character", false, MENU_PRIORITY)]
        private static void CreateCharacter(MenuCommand command) => CreateEntity(command, FieldBodyPreset.Character);

        [MenuItem(MENU + "Entity/Player Character", false, MENU_PRIORITY)]
        private static void CreatePlayerCharacter(MenuCommand command) => CreateEntity(command, FieldBodyPreset.PlayerCharacter);

        [MenuItem(MENU + "Entity/Interactable Object", false, MENU_PRIORITY)]
        private static void CreateInteractableObject(MenuCommand command) => CreateEntity(command, FieldBodyPreset.InteractableObject);

        [MenuItem(MENU + "Entity/Examine Point", false, MENU_PRIORITY)]
        private static void CreateExaminePoint(MenuCommand command) => CreateEntity(command, FieldBodyPreset.ExaminePoint);

        [MenuItem(MENU + "Entity/Area", false, MENU_PRIORITY)]
        private static void CreateArea(MenuCommand command) => CreateEntity(command, FieldBodyPreset.Area);

        [MenuItem(MENU + "Entity/Save Point", false, MENU_PRIORITY)]
        private static void CreateSavePoint(MenuCommand command) => CreateEntity(command, FieldBodyPreset.SavePoint);

        [MenuItem(MENU + "Entity/Plain", false, MENU_PRIORITY)]
        private static void CreatePlain(MenuCommand command) => CreateEntity(command, FieldBodyPreset.Plain);

        [MenuItem(MENU + "Blocker", false, MENU_PRIORITY)]
        private static void CreateBlocker(MenuCommand command)
        {
            GameObject    context = command.context as GameObject;
            FieldEntities field   = FindField(context);
            FieldBlocker  blocker = BuildBlocker(field.gameObject, field.Dimension);

            Place(blocker.gameObject, context, field, "Create Blocker");
        }

        [MenuItem(MENU + "Spawn Point", false, MENU_PRIORITY)]
        private static void CreateSpawnPoint(MenuCommand command)
        {
            GameObject    context = command.context as GameObject;
            FieldEntities field   = FindField(context);
            int           id      = SpawnPoint.NextId(field.transform, null);

            GameObject spawnObject = new GameObject($"SpawnPoint{id}");
            spawnObject.AddComponent<SpawnPoint>().SetId(id);

            Place(spawnObject, context, field, "Create Spawn Point");
        }

        /// <summary>
        /// Everything here belongs to a field, so the items are offered only inside one: under a
        /// <see cref="FieldEntities" /> root, or in the prefab view of one.
        /// </summary>
        [MenuItem(MENU + "Entity/Gateway",            true)]
        [MenuItem(MENU + "Entity/Character",          true)]
        [MenuItem(MENU + "Entity/Player Character",   true)]
        [MenuItem(MENU + "Entity/Interactable Object", true)]
        [MenuItem(MENU + "Entity/Examine Point",      true)]
        [MenuItem(MENU + "Entity/Area",               true)]
        [MenuItem(MENU + "Entity/Save Point",         true)]
        [MenuItem(MENU + "Entity/Plain",              true)]
        [MenuItem(MENU + "Blocker",                   true)]
        [MenuItem(MENU + "Spawn Point",               true)]
        private static bool IsInField()
        {
            bool isInField = FindField(Selection.activeGameObject) != null;

            return isInField;
        }

        private static void CreateEntity(MenuCommand command, FieldBodyPreset preset)
        {
            GameObject    context = command.context as GameObject;
            FieldEntities field   = FindField(context);
            GameObject    visuals = DefaultVisuals(preset, field.Dimension);

            Undo.RecordObject(field, $"Create {preset}");

            FieldEntityRecord entity = CreateEntityRecord(field, preset.ToString());
            FieldEntity       body   = Build(field.gameObject, field.Dimension, preset, entity.Name, visuals);

            entity.SetBody(body);
            WritePresetScripts(entity, preset, visuals != null);

            EditorUtility.SetDirty(field);

            Place(body.gameObject, context, field, $"Create {preset}");
        }

        private static FieldEntities FindField(GameObject context)
        {
            FieldEntities field = context != null ? context.GetComponentInParent<FieldEntities>(true) : null;

            if (field == null)
            {
                PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
                field = stage != null ? stage.prefabContentsRoot.GetComponent<FieldEntities>() : null;
            }

            return field;
        }

        /// <summary>
        /// Under the object right-clicked if it is in this field, otherwise the field's root, at its parent's origin.
        /// </summary>
        private static void Place(GameObject created, GameObject context, FieldEntities field, string undoName)
        {
            GameObject parent = context != null && context.GetComponentInParent<FieldEntities>(true) == field ? context : field.gameObject;

            GameObjectUtility.SetParentAndAlign(created, parent);

            Undo.RegisterCreatedObjectUndo(created, undoName);
            Selection.activeGameObject = created;
        }
    }
}