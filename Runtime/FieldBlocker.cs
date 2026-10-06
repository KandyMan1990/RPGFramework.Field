using UnityEngine;
using UnityEngine.Tilemaps;

namespace RPGFramework.Field
{
    /// <summary>
    /// A part of the field a script can close and open again, as a story flag closes a path. Whatever blocks lives on
    /// this GameObject or under it — solid colliders for the physics drivers, a tilemap of blocked cells for the tilemap
    /// driver — and switching the blocker switches the GameObject, so all of it goes together, visuals included. It
    /// starts the way the prefab has it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FieldBlocker : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("How scripts name this blocker. Unique within the field")]
        private byte m_Id;

        private Tilemap m_BlockedCells;

        internal byte Id => m_Id;

        internal bool IsBlocking => gameObject.activeInHierarchy;

        internal void Bind()
        {
            m_BlockedCells = GetComponentInChildren<Tilemap>(true);
        }

        internal void SetBlocking(bool blocking)
        {
            gameObject.SetActive(blocking);
        }

        /// <summary>
        /// For the tilemap driver, which has no colliders to meet: whether this blocker closes the cell at
        /// <paramref name="worldPosition" />.
        /// </summary>
        internal bool BlocksCell(Vector3 worldPosition)
        {
            bool blocks = IsBlocking && m_BlockedCells != null && m_BlockedCells.HasTile(m_BlockedCells.WorldToCell(worldPosition));

            return blocks;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Authoring only. One above the highest id among the other blockers in the field under
        /// <paramref name="fieldRoot" />, so two added one after another never share one.
        /// </summary>
        internal static byte NextId(Transform fieldRoot, FieldBlocker except)
        {
            int highest = -1;

            FieldBlocker[] others = fieldRoot.GetComponentsInChildren<FieldBlocker>(true);

            for (int i = 0; i < others.Length; i++)
            {
                FieldBlocker other = others[i];

                if (other != except && other.m_Id > highest)
                {
                    highest = other.m_Id;
                }
            }

            byte nextId = (byte)(highest + 1);

            return nextId;
        }

        internal void SetId(byte id)
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
