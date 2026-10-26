using System.Collections.Generic;
using RPGFramework.Field.FieldVmArgs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RPGFramework.Field
{
    /// <summary>
    /// Moves a body a tile at a time on a 2D tile field's walkable tilemap, in the four directions of the tilemap's own
    /// grid. A tile is entered only where the tilemap has one, no blocker closes it and nothing solid stands on it —
    /// another character, or a wall of colliders — and walking at one that cannot be entered still faces it. A step under
    /// way is finished; holding the input on starts the next as it ends. The body may keep a rigidbody, for its triggers.
    /// </summary>
    internal sealed class TilemapMovementDriver : MonoBehaviour, IMovementDriver
    {
        private const float MIN_MOVE_INPUT_SQR = 0.0001f;

        // Narrower than a tile, so what stands on the tiles beside the one asked about is not counted, and anything
        // stepping onto it is.
        private const float OCCUPIED_FRACTION = 0.9f;

        private static readonly ContactFilter2D m_SolidOnly = new ContactFilter2D { useTriggers = false };

        private readonly List<Collider2D>  m_Overlaps = new List<Collider2D>(8);
        private readonly List<FieldEntity> m_Pushed   = new List<FieldEntity>();

        private Transform                   m_Transform;
        private Tilemap                     m_Tilemap;
        private IReadOnlyList<FieldBlocker> m_Blockers;
        private Collider2D[]                m_OwnColliders;
        private float                       m_Speed;
        private Vector3                     m_MoveInput;
        private Vector3                     m_Target;
        private bool                        m_Moving;
        private ScriptedMove                m_ScriptedMove;
        private SpriteFacing                m_Facing;

        public void Init(Transform entityTransform, Tilemap tilemap, IReadOnlyList<FieldBlocker> blockers, float speed)
        {
            m_Transform    = entityTransform;
            m_Tilemap      = tilemap;
            m_Blockers     = blockers;
            m_OwnColliders = entityTransform.GetComponentsInChildren<Collider2D>(true);
            m_Speed        = speed;
            m_Facing       = new SpriteFacing();
        }

        void IMovementDriver.SetMoveInput(Vector3 move)
        {
            m_MoveInput = move;

            if (!m_Moving && !m_ScriptedMove.Active)
            {
                TryStep();
            }
        }

        void IMovementDriver.SetMoveSpeed(float speed)
        {
            m_Speed = speed;
        }

        void IMovementDriver.Tick(float deltaTime)
        {
            HandleMovement(deltaTime);
            m_Facing.Tick(deltaTime);
        }

        void IMovementDriver.PhysicsTick(float fixedDeltaTime)
        {
            // noop - this driver moves a transform, which is per-frame work.
        }

        void IMovementDriver.SetPosition(Vector3 position)
        {
            m_Transform.position = position;
        }

        void IMovementDriver.SetRotation(Quaternion rotation)
        {
            m_Facing.SetRotation(rotation);
        }

        Quaternion IMovementDriver.Rotation => m_Facing.Rotation;

        Vector3 IMovementDriver.Facing => m_Facing.Facing;

        void IMovementDriver.Face(Vector3 direction)
        {
            m_Facing.Face(direction);
        }

        void IMovementDriver.StartRotation(SetEntityRotationAsyncArgs args)
        {
            m_Facing.StartTurn(args);
        }

        void IMovementDriver.ResumeRotation(RotationState rotationState)
        {
            m_Facing.ResumeTurn(rotationState);
        }

        RotationState IMovementDriver.GetRotationState()
        {
            return m_Facing.Turn;
        }

        Vector3 IMovementDriver.CurrentVelocity
        {
            get
            {
                Vector3 currentVelocity = m_ScriptedMove.Active ? (m_ScriptedMove.Target - m_Transform.position).normalized * m_Speed :
                                          m_Moving              ? (m_Target - m_Transform.position).normalized * m_Speed : Vector3.zero;

                return currentVelocity;
            }
        }

        /// <summary>
        /// A scripted move goes straight to its point rather than tile by tile, so it can finish between tiles; the
        /// next step the player takes starts from the tile it ended in. A step already under way gives way to it.
        /// </summary>
        void IMovementDriver.MoveTo(Vector3 target, float stopDistance, bool faceTravel)
        {
            m_Moving = false;
            m_Pushed.Clear();

            m_ScriptedMove = new ScriptedMove
                             {
                                 Active       = true,
                                 Target       = target,
                                 StopDistance = stopDistance,
                                 FaceTravel   = faceTravel
                             };
        }

        void IMovementDriver.StopMove()
        {
            m_ScriptedMove.Active = false;
        }

        bool IMovementDriver.IsMovingToTarget => m_ScriptedMove.Active;

        /// <summary>What stands on the tile last walked at, solid or not, for as long as it is walked at.</summary>
        IReadOnlyList<FieldEntity> IMovementDriver.Pushed => m_Pushed;

        private void TryStep()
        {
            m_Pushed.Clear();

            Vector3Int step = Quantize(m_MoveInput);

            if (step == Vector3Int.zero)
            {
                return;
            }

            Vector3Int cell   = m_Tilemap.WorldToCell(m_Transform.position);
            Vector3Int next   = cell + step;
            Vector3    target = m_Tilemap.GetCellCenterWorld(next);

            m_Facing.Face(target - m_Tilemap.GetCellCenterWorld(cell));

            if (!CanEnter(next, target))
            {
                return;
            }

            m_Target = target;
            m_Moving = true;
        }

        private bool CanEnter(Vector3Int cell, Vector3 centre)
        {
            if (!m_Tilemap.HasTile(cell))
            {
                return false;
            }

            for (int i = 0; i < m_Blockers.Count; i++)
            {
                if (m_Blockers[i].BlocksCell(centre))
                {
                    return false;
                }
            }

            return !IsOccupied(centre);
        }

        // Notes everything on the tile as pushed, as a free field's drivers note what they walk into, solid or not.
        private bool IsOccupied(Vector3 centre)
        {
            Vector3 across = GridAxis(Vector3Int.right);
            Vector3 along  = GridAxis(Vector3Int.up);
            Vector2 size   = new Vector2(across.magnitude, along.magnitude) * OCCUPIED_FRACTION;
            float   angle  = Vector2.SignedAngle(Vector2.right, across);

            int  count    = Physics2D.OverlapBox(centre, size, angle, m_SolidOnly, m_Overlaps);
            bool occupied = false;

            for (int i = 0; i < count; i++)
            {
                Collider2D other = m_Overlaps[i];

                if (other.gameObject == m_Tilemap.gameObject || System.Array.IndexOf(m_OwnColliders, other) >= 0)
                {
                    continue;
                }

                FieldEntity.AddOwnerOf(other, m_Pushed);

                occupied |= FieldEntity.Blocks(other);
            }

            return occupied;
        }

        /// <summary>The nearest of the grid's four directions to the input, along the tilemap's own axes.</summary>
        private Vector3Int Quantize(Vector3 move)
        {
            float x = Vector3.Dot(move, GridAxis(Vector3Int.right).normalized);
            float y = Vector3.Dot(move, GridAxis(Vector3Int.up).normalized);

            if (x * x + y * y < MIN_MOVE_INPUT_SQR)
            {
                return Vector3Int.zero;
            }

            if (math.abs(x) > math.abs(y))
            {
                return x > 0f ? Vector3Int.right : Vector3Int.left;
            }

            return y > 0f ? Vector3Int.up : Vector3Int.down;
        }

        /// <summary>One tile's step along a grid direction, in the world.</summary>
        private Vector3 GridAxis(Vector3Int direction)
        {
            return m_Tilemap.CellToWorld(direction) - m_Tilemap.CellToWorld(Vector3Int.zero);
        }

        private void HandleMovement(float deltaTime)
        {
            if (m_ScriptedMove.Active)
            {
                HandleScriptedMove(deltaTime);

                return;
            }

            if (!m_Moving)
            {
                return;
            }

            m_Transform.position = Vector3.MoveTowards(m_Transform.position, m_Target, m_Speed * deltaTime);

            if (Vector3.Distance(m_Transform.position, m_Target) < 0.001f)
            {
                m_Transform.position = m_Target;
                m_Moving             = false;

                TryStep();
            }
        }

        private void HandleScriptedMove(float deltaTime)
        {
            Vector3 motion = m_ScriptedMove.Motion(m_Transform.position, m_Speed * deltaTime);

            m_Transform.position += motion;

            if (m_ScriptedMove.FaceTravel)
            {
                m_Facing.Face(motion);
            }

            if (m_ScriptedMove.HasArrived(m_Transform.position))
            {
                m_ScriptedMove.Active = false;
            }
        }
    }
}
