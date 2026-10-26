using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RPGFramework.Field.Editor
{
    /// <summary>
    /// The GameObject menu's field cameras, one for each way a field is seen: fixed, or following the player within
    /// bounds. Each is a Cinemachine camera, whose view the field module scene's camera takes.
    /// </summary>
    internal static class FieldCameraBuilder
    {
        private const string MENU          = "GameObject/RPG Framework/Field/Camera/";
        private const int    MENU_PRIORITY = 10;

        private const float DISTANCE_2D          = 10f;
        private const float ORTHOGRAPHIC_SIZE_2D = 5f;
        private const float DISTANCE_3D          = 10f;

        private static readonly Vector2 m_Area2D   = new Vector2(20f, 12f);
        private static readonly Vector3 m_Bounds3D = new Vector3(10f, 10f, 10f);

        [MenuItem(MENU + "2D Fixed", false, MENU_PRIORITY)]
        private static void Create2DFixed(MenuCommand command)
        {
            GameObject        context = command.context as GameObject;
            FieldEntities     field   = FieldEntityBodyBuilder.FindField(context);
            Bounds            area    = Area2D(field);
            CinemachineCamera camera  = NewCamera("Fixed Camera", null);

            camera.Lens.OrthographicSize = ORTHOGRAPHIC_SIZE_2D;

            FieldEntityBodyBuilder.Place(camera.gameObject, context, field, "Create 2D Fixed Camera");

            camera.transform.SetPositionAndRotation(area.center + Vector3.back * DISTANCE_2D, Quaternion.identity);
        }

        [MenuItem(MENU + "2D Follow", false, MENU_PRIORITY)]
        private static void Create2DFollow(MenuCommand command)
        {
            GameObject    context = command.context as GameObject;
            FieldEntities field   = FieldEntityBodyBuilder.FindField(context);
            Bounds        area    = Area2D(field);
            GameObject    group   = new GameObject("Follow Camera");

            BoxCollider2D bounds = NewChild("Bounds", group).AddComponent<BoxCollider2D>();
            bounds.isTrigger = true;
            bounds.size      = area.size;

            CinemachineCamera camera = NewCamera("Camera", group);
            camera.Lens.OrthographicSize = ORTHOGRAPHIC_SIZE_2D;

            FollowPlayerWithoutLag(camera, DISTANCE_2D);

            CinemachineConfiner2D confiner = camera.gameObject.AddComponent<CinemachineConfiner2D>();
            confiner.BoundingShape2D = bounds;
            confiner.Damping         = 0f;
            confiner.SlowingDistance = 0f;

            FieldEntityBodyBuilder.Place(group, context, field, "Create 2D Follow Camera");

            bounds.transform.position = area.center;
            camera.transform.SetPositionAndRotation(area.center + Vector3.back * DISTANCE_2D, Quaternion.identity);
        }

        [MenuItem(MENU + "3D Fixed", false, MENU_PRIORITY)]
        private static void Create3DFixed(MenuCommand command)
        {
            GameObject        context = command.context as GameObject;
            FieldEntities     field   = FieldEntityBodyBuilder.FindField(context);
            CinemachineCamera camera  = NewCamera("Fixed Camera", null);

            FieldEntityBodyBuilder.Place(camera.gameObject, context, field, "Create 3D Fixed Camera");

            LookFromSceneView(camera);
        }

        [MenuItem(MENU + "3D Follow", false, MENU_PRIORITY)]
        private static void Create3DFollow(MenuCommand command)
        {
            GameObject    context = command.context as GameObject;
            FieldEntities field   = FieldEntityBodyBuilder.FindField(context);
            GameObject    group   = new GameObject("Follow Camera");

            BoxCollider bounds = NewChild("Bounds", group).AddComponent<BoxCollider>();
            bounds.isTrigger = true;
            bounds.size      = m_Bounds3D;

            CinemachineCamera camera = NewCamera("Camera", group);

            SceneView sceneView = SceneView.lastActiveSceneView;

            FollowPlayerWithoutLag(camera, sceneView != null ? sceneView.cameraDistance : DISTANCE_3D);

            CinemachineConfiner3D confiner = camera.gameObject.AddComponent<CinemachineConfiner3D>();
            confiner.BoundingVolume  = bounds;
            confiner.SlowingDistance = 0f;

            FieldEntityBodyBuilder.Place(group, context, field, "Create 3D Follow Camera");

            LookFromSceneView(camera);

            bounds.transform.position = camera.transform.position;
        }

        [MenuItem(MENU + "2D Fixed",  true)]
        [MenuItem(MENU + "2D Follow", true)]
        private static bool IsIn2DField()
        {
            return IsInField(FieldDimension.TwoD);
        }

        [MenuItem(MENU + "3D Fixed",  true)]
        [MenuItem(MENU + "3D Follow", true)]
        private static bool IsIn3DField()
        {
            return IsInField(FieldDimension.ThreeD);
        }

        private static bool IsInField(FieldDimension dimension)
        {
            FieldEntities field = FieldEntityBodyBuilder.FindField(Selection.activeGameObject);

            return field != null && field.Dimension == dimension;
        }

        private static CinemachineCamera NewCamera(string name, GameObject parent)
        {
            GameObject cameraObject = parent != null ? NewChild(name, parent) : new GameObject(name);

            return cameraObject.AddComponent<CinemachineCamera>();
        }

        private static GameObject NewChild(string name, GameObject parent)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);

            return child;
        }

        /// <summary>Locked onto the player, kept at the centre of the screen.</summary>
        private static void FollowPlayerWithoutLag(CinemachineCamera camera, float distance)
        {
            CinemachinePositionComposer composer = camera.gameObject.AddComponent<CinemachinePositionComposer>();
            composer.Damping        = Vector3.zero;
            composer.CameraDistance = distance;

            camera.gameObject.AddComponent<FollowPlayer>();
        }

        /// <summary>What the field's tilemaps cover, or a screen's worth around its root if it has none.</summary>
        private static Bounds Area2D(FieldEntities field)
        {
            TilemapRenderer[] tilemaps = field.GetComponentsInChildren<TilemapRenderer>(true);

            if (tilemaps.Length == 0)
            {
                return new Bounds(field.transform.position, m_Area2D);
            }

            Bounds area = tilemaps[0].bounds;

            for (int i = 1; i < tilemaps.Length; i++)
            {
                area.Encapsulate(tilemaps[i].bounds);
            }

            // A tilemap's bounds are a cell deep; the field is flat.
            area.center = new Vector3(area.center.x, area.center.y, field.transform.position.z);

            return area;
        }

        /// <summary>From where the scene view looks, so the camera starts out showing what the designer sees.</summary>
        private static void LookFromSceneView(CinemachineCamera camera)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;

            if (sceneView == null)
            {
                return;
            }

            Transform view = sceneView.camera.transform;

            camera.transform.SetPositionAndRotation(view.position, view.rotation);
            camera.Lens.FieldOfView = sceneView.camera.fieldOfView;
        }
    }
}
