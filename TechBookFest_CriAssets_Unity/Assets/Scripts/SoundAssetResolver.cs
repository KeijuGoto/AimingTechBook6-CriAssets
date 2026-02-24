using System.Linq;

namespace TbfCa
{
    /// <summary>
    /// キューシート名や Addressable アセットのキーを元に CRI アセットのキーやパスへ変換します.
    /// プロジェクトの構成や運用ルールに応じて、適宜実装を変更してください.
    /// </summary>
    public class SoundAssetResolver
    {
        public static string KeyToAssetPath(string assetKey)
        {
            if (ContainsAddress(assetKey))
            {
                // assetKey と CRI アセットのパスが一致しない場合は、ここに変換処理を実装してください.
                return assetKey;
            }

            var assetPathWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(assetKey);
            var extension = System.IO.Path.GetExtension(assetKey);
            return $"Assets/CriAssets/{assetPathWithoutExtension}{extension}";
        }

        public static string GetAcbAssetKey(string cueSheetName)
        {
            return $"Assets/CriAssets/Addressables/{cueSheetName}.acb";
        }

        public static string GetAwbAssetKeyOrNull(string cueSheetName)
        {
            // ACB ファイルに対して AWB ファイルが存在するか、どのように判定するかはプロジェクト毎に調整してください.
            // ここでは名前が "BGM_" で始まるキューシートには必ず AWB ファイルが存在するというルールにしています.
            return cueSheetName.StartsWith("BGM_") ? $"Assets/CriAssets/Addressables/{cueSheetName}.awb" : null;
        }

        private static bool ContainsAddress(string key)
        {
#if UNITY_EDITOR
            var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return false;

            // キーを全件調べて group に追加されているか判定する. プロジェクトが肥大化してきたら調べる group を絞る必要がある
            return settings.groups
                .Where(group => group != null)
                .SelectMany(group => group.entries)
                .Any(entry => entry.address == key);
#else
            return true;
#endif
        }
    }
}