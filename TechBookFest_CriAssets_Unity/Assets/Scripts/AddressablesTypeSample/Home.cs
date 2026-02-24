using System.Linq;
using System.Threading;
using CriWare;
using CriWare.Assets;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace TbfCa.AddressablesTypeSample
{
    public class Home : MonoBehaviour
    {
        [SerializeField] private Button _bgmButton;
        [SerializeField] private Button _playTimelineButton;
        [SerializeField] private Button _backButton;

        [SerializeField] private CriAtomSourceBase _bgmSource;
        [SerializeField] private AssetReference _timelineAssetReference;
        [SerializeField] private Transform _timelineContainer;

        private void Start()
        {
            _bgmButton.onClick.AddListener(() =>  _bgmSource.Play());
            _playTimelineButton.onClick.AddListener(() => PlayTimelineAsync(destroyCancellationToken).Forget());
            _backButton.onClick.AddListener(() => SceneChanger.Instance.LoadSceneAsync("Title").Forget());
        }

        private async UniTask PlayTimelineAsync(CancellationToken ct)
        {
            var timelineGo = await Addressables.InstantiateAsync(_timelineAssetReference, parent: _timelineContainer);
            if (ct.IsCancellationRequested)
            {
                if (timelineGo != null)
                    Addressables.ReleaseInstance(timelineGo);

                return;
            }

            try
            {
                var timeline = timelineGo.GetComponent<PlayableDirector>();
                var acbAssets = timelineGo.GetComponentsInChildren<CriAtomSourceForAsset>(includeInactive: true)
                    .Select(source => source.Cue.AcbAsset)
                    .Where(acbAsset => acbAsset != null);
                await UniTask.WaitUntil(() => acbAssets.All(acbAsset => acbAsset.Loaded), cancellationToken: ct);
                timeline.Play();
                await UniTask.NextFrame(ct);
                await UniTask.WaitUntil(() => timeline.state == PlayState.Paused, cancellationToken: ct);
            }
            finally
            {
                if (timelineGo != null)
                    Addressables.ReleaseInstance(timelineGo);
            }
        }

        private void OnDestroy()
        {
            _bgmButton.onClick.RemoveAllListeners();
            _playTimelineButton.onClick.RemoveAllListeners();
            _backButton.onClick.RemoveAllListeners();
        }
    }
}