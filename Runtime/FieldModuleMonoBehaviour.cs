using UnityEngine;

namespace RPGFramework.Field
{
    public class FieldModuleMonoBehaviour : MonoBehaviour
    {
        public Vector3 Up => m_Up.normalized;

        [Tooltip("Used to determine the up direction in the game.  3D games should use 0,1,0 (XZ plane).  2D games should use 0,0,1 (XY plane).")]
        [SerializeField]
        private Vector3 m_Up = Vector3.up;
    }
}