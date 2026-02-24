using UnityEngine;
using UnityEngine.EventSystems;

namespace TbfCa
{
    public class GlobalEventSystemGenerator : MonoBehaviour
    {
        [SerializeField] private EventSystem _eventSystemPrefab;
        private static bool _exists;

        private void Awake()
        {
            if (_exists) return;

            var instance = Instantiate(_eventSystemPrefab);
            DontDestroyOnLoad(instance);
            _exists = true;
        }
    }
}