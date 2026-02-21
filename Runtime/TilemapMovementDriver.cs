using UnityEngine;
using UnityEngine.Tilemaps;

namespace RPGFramework.Field
{
    public sealed class TilemapMovementDriver : MonoBehaviour, IMovementDriver
    {
        private Transform m_Transform;
        private Tilemap   m_Tilemap;

        private Vector3 m_Target;
        private bool    m_Moving;

        public void Init(Transform entityTransform, Tilemap tilemap)
        {
            m_Transform = entityTransform;
            m_Tilemap   = tilemap;
        }

        public void SetMoveInput(Vector3 move)
        {
            if (m_Moving)
            {
                return;
            }

            Vector3Int cell = m_Tilemap.WorldToCell(m_Transform.position);

            Vector3Int dir = Quantize(move);

            if (dir == Vector3Int.zero)
            {
                return;
            }

            Vector3Int next = cell + dir;

            if (!m_Tilemap.HasTile(next))
            {
                return;
            }

            m_Target = m_Tilemap.GetCellCenterWorld(next);
            m_Moving = true;
        }

        public void Tick(float deltaTime)
        {
            if (!m_Moving)
            {
                return;
            }

            m_Transform.position = Vector3.MoveTowards(m_Transform.position, m_Target, 6f * deltaTime);
            m_Transform.forward  = (m_Target - m_Transform.position).normalized;

            if (Vector3.Distance(m_Transform.position, m_Target) < 0.001f)
            {
                m_Transform.position = m_Target;
                m_Moving             = false;
            }
        }

        private static Vector3Int Quantize(Vector3 move)
        {
            if (Mathf.Abs(move.x) > Mathf.Abs(move.z))
            {
                return move.x > 0 ? Vector3Int.right : Vector3Int.left;
            }

            return move.z > 0 ? Vector3Int.up : Vector3Int.down;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }
    }
}