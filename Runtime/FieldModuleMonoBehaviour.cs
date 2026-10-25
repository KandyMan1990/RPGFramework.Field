using RPGFramework.Core;
using RPGFramework.Core.Input;
using RPGFramework.DI;
using RPGFramework.Field.SharedTypes;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGFramework.Field
{
    internal sealed class FieldModuleMonoBehaviour : MonoBehaviour, IModuleBindings
    {
        public Vector3         Up                     => m_Up.normalized;
        public float           PlayerInteractionAngle => m_PlayerInteractionAngle;
        public InputAdapter    InputAdapter           => m_InputAdapter;
        public UIDocument      UIDocument             => m_UIDocument;
        public TransformHandle CameraTransformHandle  => m_Camera.transformHandle;
        public Camera          Camera                 => m_Camera;
        public bool            VisibleWhileSuspended  => m_VisibleWhileSuspended;

        /// <summary>
        /// Which way is up the screen for a camera looking the usual way over the field, along the ground: forward in a
        /// 3D field, up in a 2D one. A facing is measured from it.
        /// </summary>
        public Vector3 North
        {
            get
            {
                Vector3 north = Vector3.ProjectOnPlane(Vector3.forward, Up);

                return north.sqrMagnitude > 0.0001f ? north.normalized : Vector3.ProjectOnPlane(Vector3.up, Up).normalized;
            }
        }

        [Tooltip("Used to determine the up direction in the game.  3D games should use 0,1,0 (XZ plane).  2D games should use 0,0,1 (XY plane).")]
        [SerializeField]
        private Vector3 m_Up = Vector3.up;

        [Tooltip("Used to determine how wide the player interaction angle is.  Most likely should be between 45 and 90 degrees.")]
        [SerializeField]
        [Range(0f, 360f)]
        private float m_PlayerInteractionAngle = 60f;

        [SerializeField]
        private InputAdapter m_InputAdapter;

        [SerializeField]
        private UIDocument m_UIDocument;

        [SerializeField]
        private Camera m_Camera;

        [Tooltip("Keep the field on screen, frozen, under a module opened over it, such as the menu. Off, its camera is switched off until the field resumes.")]
        [SerializeField]
        private bool m_VisibleWhileSuspended;

        void IModuleBindings.Bind(IDIContainer sceneContainer)
        {
            sceneContainer.BindSingletonIfNotRegistered<IFieldPresentation, PrefabFieldPresentation>();
            sceneContainer.BindSingletonIfNotRegistered<IFieldModule, FieldModule>();
        }
    }
}