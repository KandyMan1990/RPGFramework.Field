using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace RPGFramework.Field.StreamingAssetLoader
{
    internal interface IStreamingAssetLoader
    {
        Task<byte[]> LoadAsync(string path);
    }
    
    internal static class StreamingAssetLoaderProvider
    {
        internal static IStreamingAssetLoader Get()
        {
#if (UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
            return new WebStreamingAssetLoader();
#else
            return new FileStreamingAssetLoader();
#endif
        }
    }
    
    internal sealed class FileStreamingAssetLoader : IStreamingAssetLoader
    {
        async Task<byte[]> IStreamingAssetLoader.LoadAsync(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"{nameof(FileStreamingAssetLoader)}::{nameof(IStreamingAssetLoader.LoadAsync)} File not found at path [{path}]");
            }

            return await File.ReadAllBytesAsync(path);
        }
    }
    
    internal sealed class WebStreamingAssetLoader : IStreamingAssetLoader
    {
        async Task<byte[]> IStreamingAssetLoader.LoadAsync(string path)
        {
            using UnityWebRequest req = UnityWebRequest.Get(path);

            await req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                throw new IOException($"{nameof(WebStreamingAssetLoader)}::{nameof(IStreamingAssetLoader.LoadAsync)} Status [{req.result}] when requesting file at path [{path}]");
            }

            return req.downloadHandler.data;
        }
    }
}