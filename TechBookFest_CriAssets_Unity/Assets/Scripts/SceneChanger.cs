using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace TbfCa
{
    /// <summary>
    /// ビルトインシーンと Addressable なシーン間を遷移(Load & Unload)します。
    /// ※注意: 初回のシーン遷移はビルトインシーンから行ってください
    /// </summary>
    public class SceneChanger : MonoBehaviour
    {
        private static SceneChanger _instance;
        private readonly string[] BuiltInSceneName = { "Title", "BuiltInScene" };

        public static SceneChanger Instance
        {
            get
            {
                if (_instance != null) return _instance;

                var go = new GameObject(nameof(SceneChanger));
                _instance = go.AddComponent<SceneChanger>();
                DontDestroyOnLoad(_instance);
                return Instance;
            }
        }

        private AsyncOperationHandle _currentScene;

        public async UniTask LoadSceneAsync(string key, CancellationToken ct = default)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken, ct);

            // ビルトインシーンは Addressables を使わずロード
            var sceneName = Path.GetFileNameWithoutExtension(key);
            if (BuiltInSceneName.Contains(sceneName))
            {
                await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                if (cts.IsCancellationRequested)
                {
                    await SceneManager.UnloadSceneAsync(sceneName);
                    return;
                }

                if (_currentScene.IsValid())
                    await Addressables.UnloadSceneAsync(_currentScene, autoReleaseHandle: true);

                return;
            }

            var handle = Addressables.LoadSceneAsync(key, LoadSceneMode.Additive);
            await handle;

            if (cts.IsCancellationRequested)
            {
                if (handle.IsValid())
                    await Addressables.UnloadSceneAsync(handle, autoReleaseHandle: true);

                return;
            }

            if (_currentScene.IsValid())
            {
                await Addressables.UnloadSceneAsync(_currentScene, autoReleaseHandle: true);
            }
            else
            {
                // 起動時のシーンは Addressables を使わずアンロード
                var currentActiveScene = SceneManager.GetActiveScene();
                if (BuiltInSceneName.Contains(currentActiveScene.name))
                {
                    await SceneManager.UnloadSceneAsync(currentActiveScene);
                }
            }

            _currentScene = handle;
        }
    }
}