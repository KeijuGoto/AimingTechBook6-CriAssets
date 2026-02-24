using CriWare;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace TbfCa.StreamingAssetsTypeSample
{
    public class AddressableScene : MonoBehaviour
    {
        [SerializeField] private Button _bgmButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private CriAtomSourceBase _player;

        private void Start()
        {
            _bgmButton.onClick.AddListener(() =>  _player.Play());
            _backButton.onClick.AddListener(() => SceneChanger.Instance.LoadSceneAsync("BuiltInScene").Forget());
        }

        private void OnDestroy()
        {
            _bgmButton.onClick.RemoveAllListeners();
            _backButton.onClick.RemoveAllListeners();
        }
    }
}