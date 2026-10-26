using UnityEngine;
using UnityEngine.Tilemaps;

namespace RPGFramework.Field
{
    /// <summary>
    /// Makes a 2D field a tile field: every character in it steps a tile at a time onto this tilemap's tiles, keeping
    /// its rigidbody for triggers. A field without one is walked freely.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Tilemap))]
    internal sealed class WalkableTilemap : MonoBehaviour
    {
        internal Tilemap Tilemap => GetComponent<Tilemap>();
    }
}
