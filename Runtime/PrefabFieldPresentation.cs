using System.Threading.Tasks;
using UnityEngine;

namespace RPGFramework.Field
{
    public sealed class PrefabFieldPresentation : IFieldPresentation
    {
        private GameObject  m_Instance;
        private AssetBundle m_AssetBundle;

        async Task<GameObject> IFieldPresentation.LoadAsync(FieldDatabaseAsset asset)
        {
            AssetBundleCreateRequest bundleRequest = AssetBundle.LoadFromFileAsync(asset.AssetPath);
            await bundleRequest;
            
            AssetBundleRequest prefabRequest = bundleRequest.assetBundle.LoadAssetWithSubAssetsAsync<GameObject>(asset.AssetName);
            await prefabRequest;
            
            m_AssetBundle = bundleRequest.assetBundle;

            GameObject prefab = (GameObject)prefabRequest.asset;

            GameObject[] op = await Object.InstantiateAsync(prefab);
            m_Instance = op[0];

            return m_Instance;
        }

        async Task IFieldPresentation.Unload()
        {
            Object.Destroy(m_Instance);
            AssetBundleUnloadOperation op = m_AssetBundle.UnloadAsync(true);
            
            await op;
        }
    }
}