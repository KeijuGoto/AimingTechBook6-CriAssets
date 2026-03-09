using System.Linq;
using System.Threading;
using CriWare;
using CriWare.Assets;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace TbfCa
{
    /// <summary>
    /// キューシートを動的にロードして再生するだけの最小限の機能
    /// </summary>
    public class MinimumSoundPlayer : MonoBehaviour
    {
        [SerializeField] private CriAtomSourceForAsset _source;

        public async UniTask PlayAsync(CriAtomCueReference cueReference, CancellationToken ct)
        {
            // 再生前にキューシートをロードする
            var acbAsset = cueReference.AcbAsset;
            CriAtomAssetsLoader.AddCueSheet(acbAsset);
            var cts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);
            await UniTask.WaitUntil(() => acbAsset.Loaded, cancellationToken: cts.Token);
            await PlayImplAsync(cueReference, cts.Token);
        }

        public async UniTask PlayAsync(string cueSheetName, string cueName, CancellationToken ct)
        {
            // CRI アセットをロードする.
            // cueSheetName -> assetKey の変換はプロジェクトの事情に合わせて書き換えてください
            var acbAssetKey = SoundAssetResolver.GetAcbAssetKey(cueSheetName);
            var acbAssetLoadHandle = Addressables.LoadAssetAsync<CriAtomAcbAsset>(acbAssetKey);
            await acbAssetLoadHandle;

            // 再生前にキューシートをロードする
            CriAtomAssetsLoader.AddCueSheet(acbAssetLoadHandle.Result);
            var cts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);
            await UniTask.WaitUntil(() => acbAssetLoadHandle.Result.Loaded, cancellationToken: cts.Token);

            try
            {
                // キュー名が一致するキューIDを探す
                var cueInfoList = acbAssetLoadHandle.Result.Handle.GetCueInfoList();
                var cueId = cueInfoList.First(cue => cue.name == cueName).id;
                var cueReference = new CriAtomCueReference(acbAssetLoadHandle.Result, cueId);
                await PlayImplAsync(cueReference, cts.Token);
            }
            finally
            {
                acbAssetLoadHandle.Release();
            }
        }

        private async UniTask PlayImplAsync(CriAtomCueReference cueReference, CancellationToken ct)
        {
            _source.Cue = cueReference;
            try
            {
                var playback = _source.Play();
                await UniTask.WaitUntil(() => playback.status == CriAtomExPlayback.Status.Removed,
                    cancellationToken: ct);
            }
            finally
            {
                // 再生が終わったらキューシートをアンロードする
                if (cueReference.AcbAsset.Loaded)
                    CriAtomAssetsLoader.ReleaseCueSheet(cueReference.AcbAsset);
            }
        }

        public void Stop() => _source.Stop();
    }
}