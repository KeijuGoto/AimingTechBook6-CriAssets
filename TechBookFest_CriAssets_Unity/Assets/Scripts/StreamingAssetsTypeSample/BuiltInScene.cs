using CriWare;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace TbfCa.StreamingAssetsTypeSample
{
    public class BuiltInScene : MonoBehaviour
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _goToAddressableSceneButton;
        [SerializeField] private CriAtomSourceBase _player;

        private void Start()
        {
            _playButton.onClick.AddListener(() => _player.Play());
            _goToAddressableSceneButton.onClick.AddListener(() =>
                SceneChanger.Instance.LoadSceneAsync("Assets/Addressables/Scenes/AddressableScene.unity").Forget());
        }

        private void OnDestroy()
        {
            _playButton.onClick.RemoveAllListeners();
            _goToAddressableSceneButton.onClick.RemoveAllListeners();
        }
    }
}