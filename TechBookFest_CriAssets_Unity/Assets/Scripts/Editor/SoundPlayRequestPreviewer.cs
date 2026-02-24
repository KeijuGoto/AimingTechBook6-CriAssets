using System;
using System.IO;
using System.Linq;
using CriWare;
using CriWare.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace TbfCa.Editor
{
    public class SoundPlayRequestPreviewer : IDisposable
    {
        private CriAtomEditorUtilities.PreviewPlayer PreviewPlayer { get; set; }
        private CriAtomExAcb PreviewAcb { get; set; }

        public float GetPropertyDrawerHeight() => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        public void Draw(Rect position, string cueSheetName, int? cueId = null, string cueName = "") // NOTE: cueId または cueName を指定する
        {
            var labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, "Preview");

            const float buttonWidth = 96;
            var playButtonRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, buttonWidth, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(playButtonRect,"Play"))
            {
                // NOTE: メソッド内部で多重初期化を防いでる
                CriAtomEditorUtilities.InitializeLibrary();

                if (PreviewAcb != null)
                {
                    PreviewAcb.Dispose();
                    PreviewAcb = null;
                }

                PreviewAcb = LoadAcb(cueSheetName);
                CriAtomEx.CueInfo cueInfo;
                if (cueId.HasValue)
                {
                    PreviewAcb.GetCueInfo(cueId.Value, out cueInfo);
                }
                else if (!string.IsNullOrEmpty(cueName))
                {
                    PreviewAcb.GetCueInfo(cueName, out cueInfo);
                }
                else
                {
                    Debug.LogError($"cueId または cueName を指定してください");
                    return;
                }

                // CriAtomCraft では空のキュー名を許容していないので cueInfo が取得できたかどうかを判定できる
                if (string.IsNullOrEmpty(cueInfo.name))
                {
                    Debug.LogError($"指定されたキューが見つかりません");
                    return;
                }

                if (PreviewPlayer == null)
                    PreviewPlayer = new CriAtomEditorUtilities.PreviewPlayer();

                // プレビュー用 Player は文字列でキューを指定する
                PreviewPlayer.Play(PreviewAcb, cueInfo.name);
            }

            var stopButtonRect = new Rect(playButtonRect.x + playButtonRect.width, position.y, buttonWidth, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(stopButtonRect, "Stop"))
            {
                PreviewPlayer?.Stop();
            }
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        public static CriAtomExAcb LoadAcb(string cueSheetName)
        {
            var acbAssetKey = SoundAssetResolver.GetAcbAssetKey(cueSheetName);
            var acbAssetPath = SoundAssetResolver.KeyToAssetPath(acbAssetKey);
            var acbPath = Path.GetFullPath(acbAssetPath);
            if (!File.Exists(acbPath))
                throw new FileNotFoundException($"指定されたキューシートの ACB ファイルが次の場所にありません: {acbPath}");

            var awbAssetKey = SoundAssetResolver.GetAwbAssetKeyOrNull(cueSheetName);
            var existsAwb = !string.IsNullOrEmpty(awbAssetKey);
            string awbPath = null;
            if (existsAwb)
            {
                var awbAssetPath = SoundAssetResolver.KeyToAssetPath(awbAssetKey);
                awbPath = Path.GetFullPath(awbAssetPath);
                if (!File.Exists(awbPath))
                    throw new FileNotFoundException($"指定されたキューシートの AWB ファイルが次の場所にありません: {awbPath}");
            }

            // NOTE: エディタで Load 関数を使うために必要. メソッド内部で多重初期化を防いでるのでロード前に呼び出す
            CriAtomEditorUtilities.InitializeLibrary();

            return CriAtomEditorUtilities.LoadAcbFile(null, acbPath, awbPath);
        }

        public void Dispose()
        {
            if (PreviewAcb != null)
            {
                PreviewAcb.Dispose();
                PreviewAcb = null;
            }

            if (PreviewPlayer != null)
            {
                PreviewPlayer.Dispose();
                PreviewPlayer = null;
            }
        }
    }
}