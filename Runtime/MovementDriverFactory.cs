using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RPGFramework.Field
{
    internal static class MovementDriverFactory
    {
        /// <summary>
        /// A 2D tile field moves every body a tile at a time on its <paramref name="walkableTilemap" />, a rigidbody kept
        /// for its triggers; any other field moves a body by what it has: a 3D rigidbody, a 2D one, or a plain transform.
        /// </summary>
        internal static IMovementDriver Create(GameObject gameObject, float speed, IReadOnlyList<FieldBlocker> blockers, FieldDimension dimension, Tilemap walkableTilemap)
        {
            if (walkableTilemap != null)
            {
                TilemapMovementDriver tilemapMovementDriver = gameObject.AddComponent<TilemapMovementDriver>();
                tilemapMovementDriver.Init(gameObject.transform, walkableTilemap, blockers, speed);

                return tilemapMovementDriver;
            }

            if (gameObject.TryGetComponent(out Rigidbody rb))
            {
                Rigidbody3DMovementDriver rigidbody3DMovementDriver = gameObject.AddComponent<Rigidbody3DMovementDriver>();
                rigidbody3DMovementDriver.Init(rb, speed);

                return rigidbody3DMovementDriver;
            }

            if (gameObject.TryGetComponent(out Rigidbody2D rb2d))
            {
                Rigidbody2DMovementDriver rigidbody2DMovementDriver = gameObject.AddComponent<Rigidbody2DMovementDriver>();
                rigidbody2DMovementDriver.Init(rb2d, speed);

                return rigidbody2DMovementDriver;
            }

            TransformMovementDriver transformMovementDriver = gameObject.AddComponent<TransformMovementDriver>();
            transformMovementDriver.Init(gameObject.transform, speed, dimension);

            return transformMovementDriver;
        }
    }
}
