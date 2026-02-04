using UnityEngine;

namespace RPGFramework.Field
{
    public sealed class SpawnPoint : MonoBehaviour
    {
        public int        Id       => m_Id;
        public Vector3    Position => transform.position;
        public Quaternion Rotation => transform.rotation;

        [SerializeField]
        private int m_Id;
    }
}