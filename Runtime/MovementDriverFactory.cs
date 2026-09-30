using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RPGFramework.Field
{
    internal static class MovementDriverFactory
    {
        internal static IMovementDriver Create(GameObject gameObject, float speed, IReadOnlyList<FieldBlocker> blockers)
        {
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

            Tilemap tilemap = FindWalkableTilemap();
            if (tilemap != null)
            {
                TilemapMovementDriver driver = gameObject.AddComponent<TilemapMovementDriver>();
                driver.Init(gameObject.transform, tilemap, blockers, speed);

                return driver;
            }

            TransformMovementDriver transformMovementDriver = gameObject.AddComponent<TransformMovementDriver>();
            transformMovementDriver.Init(gameObject.transform, speed);

            return transformMovementDriver;
        }

        /// <summary>
        /// The tilemap whose tiles say where an entity may walk. A blocker's tilemap says where it may not, so it is
        /// never the one.
        /// </summary>
        private static Tilemap FindWalkableTilemap()
        {
            Tilemap walkable = null;

            foreach (Tilemap tilemap in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include))
            {
                if (tilemap.GetComponentInParent<FieldBlocker>(true) == null)
                {
                    walkable = tilemap;
                    break;
                }
            }

            return walkable;
        }
    }
}