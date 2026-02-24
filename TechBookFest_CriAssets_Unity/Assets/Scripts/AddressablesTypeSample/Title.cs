using CriWare;
using Cysharp.Threading.Tasks;
using TbfCa.SoundPlayRequest;
using UnityEngine;
using UnityEngine.UI;

namespace TbfCa.AddressablesTypeSample
{
    public class Title : MonoBehaviour
    {
        [SerializeField] private Button _playWithReferenceButton;
        [SerializeField] private Button _playWithNameButton;
        [SerializeField] private Button _homeButton;

        [SerializeField] private MinimumSoundPlayer _player;
        [SerializeField] private SoundPlayRequestWithReference _referenceRequest;
        [SerializeField] private SoundPlayRequestWithName _nameRequest;

        [SerializeField, Range(0f, 1f)] private float _categoryVolume = 1f;

        private async UniTaskVoid Awake()
        {
            await UniTask.WaitUntil(CriAtomPlugin.IsLibraryInitialized, cancellationToken: destroyCancellationToken);
            CriAtomExCategory.SetVolume("BGM", _categoryVolume);
        }

        private void Start()
        {
            _playWithReferenceButton.onClick.AddListener(() => Play(_referenceRequest));
            _playWithNameButton.onClick.AddListener(() => Play(_nameRequest));
            _homeButton.onClick.AddListener(() => SceneChanger.Instance.LoadSceneAsync("Assets/Scenes/AddressablesTypeSample/Home.unity").Forget());
        }

        private void Play(ISoundPlayRequest soundPlayRequest)
        {
            switch (soundPlayRequest)
            {
                case SoundPlayRequestWithName requestWithName:
                    _player.PlayAsync(requestWithName.CueSheetName, requestWithName.CueName, destroyCancellationToken).Forget();
                    return;
                case SoundPlayRequestWithReference requestWithReference:
                    _player.PlayAsync(requestWithReference.Reference, destroyCancellationToken).Forget();
                    return;
            }
        }

        private void OnDestroy()
        {
            _playWithReferenceButton.onClick.RemoveAllListeners();
            _playWithNameButton.onClick.RemoveAllListeners();
            _homeButton.onClick.RemoveAllListeners();
        }
    }
}