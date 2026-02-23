using UnityEngine;
using UnityEngine.Tilemaps;

namespace RPGFramework.Field
{
    internal static class MovementDriverFactory
    {
        internal static IMovementDriver Create(GameObject gameObject, float speed)
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

            Tilemap tilemap = Object.FindFirstObjectByType<Tilemap>();
            if (tilemap != null)
            {
                TilemapMovementDriver driver = gameObject.AddComponent<TilemapMovementDriver>();
                driver.Init(gameObject.transform, tilemap);

                return driver;
            }

            TransformMovementDriver transformMovementDriver = gameObject.AddComponent<TransformMovementDriver>();
            transformMovementDriver.Init(gameObject.transform, speed);

            return transformMovementDriver;
        }
    }
}