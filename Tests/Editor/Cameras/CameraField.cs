using System.Collections.Generic;
using RPGFramework.Field.Editor;
using Unity.Cinemachine;
using UnityEngine;

namespace RPGFramework.Field.Tests.Cameras
{
    /// <summary>A field's root, for a test to give cameras and then check as export does.</summary>
    internal sealed class CameraField
    {
        private readonly GameObject   m_Root;
        private readonly HashSet<int> m_CameraIds;
        private          byte         m_NextId;

        internal CameraField()
        {
            m_Root      = new GameObject("Field");
            m_CameraIds = new HashSet<int>();
            m_NextId    = 0;
        }

        internal Transform Root => m_Root.transform;

        /// <summary>The ids export found, for scripts to name.</summary>
        internal IReadOnlyCollection<int> CameraIds => m_CameraIds;

        /// <summary>A camera named by the next id.</summary>
        internal CinemachineCamera AddCamera(string name)
        {
            return AddCamera(name, m_NextId++);
        }

        internal CinemachineCamera AddCamera(string name, byte id)
        {
            CinemachineCamera camera = AddCameraWithoutId(name);

            camera.gameObject.AddComponent<FieldCamera>().SetId(id);

            return camera;
        }

        internal CinemachineCamera AddCameraWithoutId(string name)
        {
            GameObject cameraObject = new GameObject(name);
            cameraObject.transform.SetParent(m_Root.transform, false);

            return cameraObject.AddComponent<CinemachineCamera>();
        }

        /// <summary>A 2D camera kept within bounds that are a trigger or not.</summary>
        internal CinemachineConfiner2D AddFollowCamera2D(bool boundsAreTrigger)
        {
            CinemachineCamera     camera   = AddCamera("Follow Camera");
            CinemachineConfiner2D confiner = camera.gameObject.AddComponent<CinemachineConfiner2D>();

            GameObject bounds = new GameObject("Bounds");
            bounds.transform.SetParent(m_Root.transform, false);

            BoxCollider2D collider = bounds.AddComponent<BoxCollider2D>();
            collider.isTrigger = boundsAreTrigger;

            confiner.BoundingShape2D = collider;

            return confiner;
        }

        /// <summary>A 3D camera kept within bounds that are a trigger or not.</summary>
        internal void AddFollowCamera3D(bool boundsAreTrigger)
        {
            CinemachineCamera     camera   = AddCamera("Follow Camera");
            CinemachineConfiner3D confiner = camera.gameObject.AddComponent<CinemachineConfiner3D>();

            GameObject bounds = new GameObject("Bounds");
            bounds.transform.SetParent(m_Root.transform, false);

            BoxCollider collider = bounds.AddComponent<BoxCollider>();
            collider.isTrigger = boundsAreTrigger;

            confiner.BoundingVolume = collider;
        }

        internal List<string> Export()
        {
            List<string> problems = new List<string>();

            m_CameraIds.Clear();
            FieldDatabase.ValidateCamera(m_Root, m_CameraIds, problems);

            return problems;
        }

        internal void Destroy()
        {
            Object.DestroyImmediate(m_Root);
        }
    }
}
