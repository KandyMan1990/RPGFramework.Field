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
    /// another character, or a wall of colliders — and walking at one that cannot be entered still faces it. A step
    /// under way is finished; holding the input on starts the next as it ends. A scripted move walks the same steps,
    /// across the grid first and then along it. The body may keep a rigidbody, for its triggers.
    /// </summary>
    internal sealed class TilemapMovementDriver : MonoBehaviour, IMovementDriver
    {
        private const float MIN_MOVE_INPUT_SQR = 0.0001f;
        private const float ARRIVAL_DISTANCE   = 0.001f;

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
        private bool                        m_ScriptedStep;
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

        Vector3 IMovementDriver.CurrentVelocity =>
            m_Moving ? (m_Target - m_Transform.position).normalized * m_Speed : Vector3.zero;

        /// <summary>
        /// Walks to the tile the point is in, a tile at a time, stopping on the first tile within the stop distance of
        /// it. A step already under way is finished first. Called again each frame to follow an entity, which only
        /// moves the point.
        /// </summary>
        void IMovementDriver.MoveTo(Vector3 target, float stopDistance, bool faceTravel)
        {
            m_Pushed.Clear();

            m_ScriptedMove = new ScriptedMove
                             {
                                 Active       = true,
                                 Target       = target,
                                 StopDistance = stopDistance,
                                 FaceTravel   = faceTravel
                             };

            if (!m_Moving)
            {
                TakeScriptedStep();
            }
        }

        /// <summary>The step under way is finished, so the move ends on a tile.</summary>
        void IMovementDriver.StopMove()
        {
            m_ScriptedMove.Active = false;
        }

        bool IMovementDriver.IsMovingToTarget => m_ScriptedMove.Active || (m_Moving && m_ScriptedStep);

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

            StartStep(target, false);
        }

        /// <summary>
        /// The next tile toward the scripted move's point: across the grid first, then along it, as a cutscene's moves
        /// read. A tile that cannot be entered is faced and tried again next frame, so a blocked move waits.
        /// </summary>
        private void TakeScriptedStep()
        {
            m_Pushed.Clear();

            Vector3Int cell   = m_Tilemap.WorldToCell(m_Transform.position);
            Vector3    centre = m_Tilemap.GetCellCenterWorld(cell);

            // Between tiles, as a move kept across a battle can be: onto the tile it is in first.
            if (m_Transform.position != centre)
            {
                FaceScriptedStep(centre - m_Transform.position);
                StartStep(centre, true);

                return;
            }

            Vector3Int goal = m_Tilemap.WorldToCell(m_ScriptedMove.Target);
            goal.z = cell.z;

            float distance = (m_Tilemap.GetCellCenterWorld(goal) - centre).magnitude;

            if (distance <= m_ScriptedMove.StopDistance + ARRIVAL_DISTANCE)
            {
                m_ScriptedMove.Active = false;

                return;
            }

            Vector3Int step = goal.x != cell.x
                ? new Vector3Int(math.sign(goal.x - cell.x), 0, 0)
                : new Vector3Int(0, math.sign(goal.y - cell.y), 0);

            Vector3Int next   = cell + step;
            Vector3    target = m_Tilemap.GetCellCenterWorld(next);

            FaceScriptedStep(target - centre);

            if (CanEnter(next, target))
            {
                StartStep(target, true);
            }
        }

        private void FaceScriptedStep(Vector3 direction)
        {
            if (m_ScriptedMove.FaceTravel)
            {
                m_Facing.Face(direction);
            }
        }

        private void StartStep(Vector3 target, bool scripted)
        {
            m_Target       = target;
            m_Moving       = true;
            m_ScriptedStep = scripted;
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
            if (m_Moving)
            {
                m_Transform.position = Vector3.MoveTowards(m_Transform.position, m_Target, m_Speed * deltaTime);

                if (Vector3.Distance(m_Transform.position, m_Target) >= ARRIVAL_DISTANCE)
                {
                    return;
                }

                m_Transform.position = m_Target;
                m_Moving             = false;

                if (!m_ScriptedMove.Active)
                {
                    TryStep();

                    return;
                }
            }

            if (m_ScriptedMove.Active)
            {
                TakeScriptedStep();
            }
        }
    }
}
