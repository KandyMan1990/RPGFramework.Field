#if ENABLE_RPG_FRAMEWORK_DEBUG
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGFramework.Field.SharedTypes;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>Reads a field's spawn points from its bundle, as entering the field reads its prefab.</summary>
    public sealed class DebugSpawnPointLoader : IDebugSpawnPointLoader
    {
        private readonly IFieldDatabase m_Database;

        internal DebugSpawnPointLoader(IFieldDatabase database)
        {
            m_Database = database;
        }

        async Task<IReadOnlyList<DebugSpawnPoint>> IDebugSpawnPointLoader.LoadAsync(ulong fieldNameHash)
        {
            FieldDefinition          field         = m_Database.Get(fieldNameHash);
            AssetBundleCreateRequest bundleRequest = AssetBundle.LoadFromFileAsync(field.AssetPath);

            await bundleRequest;

            AssetBundle        bundle        = bundleRequest.assetBundle;
            AssetBundleRequest prefabRequest = bundle.LoadAssetAsync<GameObject>(field.AssetName);

            await prefabRequest;

            SpawnPoint[]      found       = ((GameObject)prefabRequest.asset).GetComponentsInChildren<SpawnPoint>(true);
            DebugSpawnPoint[] spawnPoints = new DebugSpawnPoint[found.Length];

            for (int i = 0; i < found.Length; i++)
            {
                spawnPoints[i] = new DebugSpawnPoint(found[i].Id, found[i].name);
            }

            await bundle.UnloadAsync(true);

            return spawnPoints;
        }
    }
}
#endif
