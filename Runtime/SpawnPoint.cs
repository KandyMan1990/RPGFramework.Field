using UnityEngine;

namespace RPGFramework.Field
{
    public sealed class SpawnPoint : MonoBehaviour
    {
        public int        Id       => m_Id;
        public Vector3    Position => transform.position;
        internal Quaternion Rotation => transform.rotation;

        [SerializeField]
        private int m_Id;

#if UNITY_EDITOR
        /// <summary>
        /// Authoring only. One above the highest id among the other spawn points in the field under
        /// <paramref name="fieldRoot" />, so two added one after another never share one.
        /// </summary>
        internal static int NextId(Transform fieldRoot, SpawnPoint except)
        {
            int highest = -1;

            foreach (SpawnPoint other in fieldRoot.GetComponentsInChildren<SpawnPoint>(true))
            {
                if (other != except && other.m_Id > highest)
                {
                    highest = other.m_Id;
                }
            }

            int nextId = highest + 1;

            return nextId;
        }

        internal void SetId(int id)
        {
            m_Id = id;
        }

        private void Reset()
        {
            m_Id = NextId(transform.root, this);
        }
#endif
    }
}