using UnityEngine;

namespace RPGFramework.Field
{
    public class FieldModuleMonoBehaviour : MonoBehaviour
    {
        public Vector3 Up                     => m_Up.normalized;
        public float   PlayerInteractionAngle => m_PlayerInteractionAngle;

        [Tooltip("Used to determine the up direction in the game.  3D games should use 0,1,0 (XZ plane).  2D games should use 0,0,1 (XY plane).")]
        [SerializeField]
        private Vector3 m_Up = Vector3.up;

        [Tooltip("Used to determine how wide the player interaction angle is.  Most likely should be between 45 and 90 degrees.")]
        [SerializeField]
        [Range(0f, 360f)]
        private float m_PlayerInteractionAngle = 60f;
    }
}