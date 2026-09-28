using UnityEditor;
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
    /// Builds an entity's body: the object, the components the engine looks for, and the visuals as a prefab kept as
    /// a prefab. Assembling one by hand is how a piece gets missed, which the export checks then have to catch.
    /// </summary>
    internal static class FieldEntityBodyBuilder
    {
        private const string TRIGGER_OBJECT = "Trigger";

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
        /// The menu cannot ask what the body is for, so it makes a plain one to be wired by hand. The Field Designer
        /// is where a preset is chosen, and where the body is joined to an entity.
        /// </summary>
        [MenuItem("GameObject/RPG Framework/Field Entity Body", false, 10)]
        private static void CreateBody(MenuCommand command)
        {
            GameObject     parent        = command.context as GameObject;
            FieldEntities  fieldEntities = parent        != null ? parent.GetComponentInParent<FieldEntities>() : null;
            FieldDimension dimension     = fieldEntities != null ? fieldEntities.Dimension : FieldDimension.ThreeD;

            GameObject bodyObject = new GameObject("Field Entity");

            if (parent != null)
            {
                bodyObject.transform.SetParent(parent.transform, false);
            }

            bodyObject.AddComponent<FieldEntity>();

            Undo.RegisterCreatedObjectUndo(bodyObject, "Create Field Entity Body");
            Selection.activeGameObject = bodyObject;

            Debug.Log($"{nameof(FieldEntityBodyBuilder)} made a plain body in a {(dimension == FieldDimension.TwoD ? "2D" : "3D")} field. Give it to an entity in the Field Designer, which can also build one with its triggers and body already on it");
        }
    }
}