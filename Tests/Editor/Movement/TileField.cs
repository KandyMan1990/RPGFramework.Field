using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RPGFramework.Field.Tests.Movement
{
    /// <summary>
    /// A 2D tile field of five by five tiles, cells -2 to 2 each way, and a character with a kinematic rigidbody and a
    /// tile driver at a speed of a tile a second, for a test to walk.
    /// </summary>
    internal sealed class TileField
    {
        private readonly List<Object>       m_Made;
        private readonly List<FieldBlocker> m_Blockers;
        private readonly Tilemap            m_Tilemap;
        private readonly Tile               m_Tile;

        internal TileField(Vector3Int start)
        {
            m_Made     = new List<Object>();
            m_Blockers = new List<FieldBlocker>();
            m_Tile     = ScriptableObject.CreateInstance<Tile>();
            m_Made.Add(m_Tile);

            GameObject grid = Make("Grid");
            grid.AddComponent<Grid>();

            GameObject ground = Make("Ground");
            ground.transform.SetParent(grid.transform, false);
            m_Tilemap = ground.AddComponent<Tilemap>();

            for (int x = -2; x <= 2; x++)
            {
                for (int y = -2; y <= 2; y++)
                {
                    m_Tilemap.SetTile(new Vector3Int(x, y, 0), m_Tile);
                }
            }

            Character = Make("Character");
            Character.transform.position = Centre(start);
            Character.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            Character.AddComponent<BoxCollider2D>().size   = new Vector2(1f, 0.5f);

            TilemapMovementDriver driver = Character.AddComponent<TilemapMovementDriver>();
            driver.Init(Character.transform, m_Tilemap, m_Blockers, 1f);
            Driver = driver;
        }

        internal GameObject      Character { get; }
        internal IMovementDriver Driver    { get; }

        internal Vector3 Centre(Vector3Int cell)
        {
            return m_Tilemap.GetCellCenterWorld(cell);
        }

        /// <summary>Holds a direction for as long as it takes the character to step one tile, then lets go.</summary>
        internal void StepOnce(Vector3 direction)
        {
            Physics2D.SyncTransforms();

            Driver.SetMoveInput(direction);
            Driver.SetMoveInput(Vector3.zero);
            Driver.Tick(2f);
        }

        internal void AddSolid(Vector3Int cell, bool isTrigger)
        {
            GameObject solid = Make(isTrigger ? "Trigger" : "Wall");
            solid.transform.position = Centre(cell);

            BoxCollider2D box = solid.AddComponent<BoxCollider2D>();
            box.isTrigger = isTrigger;
        }

        /// <summary>Another field entity's body on a tile, solid or not, as a character standing there is.</summary>
        internal FieldEntity AddEntity(Vector3Int cell, bool solid)
        {
            GameObject body = Make("Entity");
            body.transform.position = Centre(cell);
            body.AddComponent<BoxCollider2D>().size = new Vector2(1f, 0.5f);

            FieldEntity entity = body.AddComponent<FieldEntity>();
            entity.SetSolid(solid);

            return entity;
        }

        internal void AddClosedBlocker(Vector3Int cell)
        {
            GameObject blockerObject = Make("Blocker");
            blockerObject.AddComponent<Grid>();

            GameObject cells = Make("Cells");
            cells.transform.SetParent(blockerObject.transform, false);
            cells.AddComponent<Tilemap>().SetTile(cell, m_Tile);

            FieldBlocker blocker = blockerObject.AddComponent<FieldBlocker>();
            blocker.Bind();
            m_Blockers.Add(blocker);
        }

        internal void Destroy()
        {
            for (int i = m_Made.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(m_Made[i]);
            }
        }

        private GameObject Make(string name)
        {
            GameObject made = new GameObject(name);
            m_Made.Add(made);

            return made;
        }
    }
}
