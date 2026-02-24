using System;
using System.Linq;
using CriWare;
using CriWare.Assets;
using CriWare.CriTimeline.Atom;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace TbfCa.Editor
{
    public class SoundComponentReplacer
    {
        [MenuItem("Sound/オブジェクト内の Source をアセット参照式に置き換え")]
        public static void ReplaceComponent()
        {
            var objects = Selection.objects.OfType<GameObject>().ToArray();
            if (!objects.Any())
            {
                Debug.LogWarning("GameObjectまたはプレハブが選択されていません。");
                return;
            }

            foreach (var @object in objects)
            {
                ReplaceComponent(@object);
            }
        }

        [MenuItem("Sound/タイムラインの Source をアセット参照式に置き換え")]
        public static void ReplaceTrackBinding()
        {
            var objects = Selection.objects.OfType<GameObject>().ToArray();
            if (!objects.Any())
            {
                Debug.LogWarning("GameObjectまたはプレハブが選択されていません。");
                return;
            }

            foreach (var @object in objects)
            {
                ReplaceComponentAndBinding(@object);
            }
        }

        [MenuItem("Sound/タイムラインの Clip をアセット参照式に置き換え")]
        public static void ReplaceClip()
        {
            var timelineAssets = Selection.objects.OfType<TimelineAsset>().ToArray();
            if (!timelineAssets.Any())
            {
                Debug.LogWarning("TimelineAsset が選択されていません。");
                return;
            }

            foreach (var timeline in timelineAssets)
            {
                ReplaceClips(timeline);
            }
        }

        private static void ReplaceComponent(GameObject gameObject)
        {
            var replaceCount = 0;
            foreach (var oldComponent in gameObject.GetComponentsInChildren<CriAtomSource>())
            {
                ReplaceComponentImpl(oldComponent);
                replaceCount++;
            }

            if (0 < replaceCount)
            {
                EditorUtility.SetDirty(gameObject);
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"置換完了: {gameObject.name} 内の {replaceCount} 個のコンポーネントを置き換えました。");
        }

        private static CriAtomSourceForAsset ReplaceComponentImpl(CriAtomSource criAtomSource)
        {
            var cueReference = GetCueReference(criAtomSource.cueSheet, criAtomSource.cueName);
            var criAtomSourceForAsset = criAtomSource.gameObject.AddComponent<CriAtomSourceForAsset>();
            criAtomSourceForAsset.Cue = cueReference;
            UnityEngine.Object.DestroyImmediate(criAtomSource, allowDestroyingAssets: true);
            return criAtomSourceForAsset;
        }

        private static void ReplaceComponentAndBinding(GameObject gameObject)
        {
            var director = gameObject.GetComponent<PlayableDirector>();
            if (director == null)
                return;

            var replaceCount = 0;
            var oldComponentBindings = director.playableAsset.outputs
                .Where(c => c.outputTargetType == typeof(CriAtomSourceBase))
                .Where(binding =>
                {
                    var boundObject = director.GetGenericBinding(binding.sourceObject) as CriAtomSource;
                    return boundObject != null;
                });
            foreach (var oldBinding in oldComponentBindings)
            {
                var oldComponent = director.GetGenericBinding(oldBinding.sourceObject) as CriAtomSource;
                var newComponent = ReplaceComponentImpl(oldComponent);
                director.ClearGenericBinding(oldBinding.sourceObject);
                director.SetGenericBinding(oldBinding.sourceObject, newComponent);
                replaceCount++;
            }

            if (0 < replaceCount)
            {
                EditorUtility.SetDirty(gameObject);
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"置換完了: {gameObject.name} 内の {replaceCount} 個のトラックに参照されたコンポーネントを置き換えました。");
        }

        private static void ReplaceClips(TimelineAsset timeline)
        {
            var replaceCount = 0;
            foreach (var track in timeline.GetOutputTracks())
            {
                var criAtomTrack = track as CriAtomTrack;
                if (criAtomTrack == null)
                    continue;

                var replaceCountInTrack = 0;
                var clips = track.GetClips().ToArray();
                foreach (var clip in clips)
                {
                    var success = ReplaceClipImpl(criAtomTrack, clip);
                    if (success) replaceCountInTrack++;
                }

                if (0 < replaceCountInTrack)
                {
                    UnityEditor.Timeline.TimelineEditor.Refresh(UnityEditor.Timeline.RefreshReason.ContentsAddedOrRemoved);
                    Debug.Log($"{track.name} 内のクリップ {replaceCountInTrack} 個を置き換えました");
                }

                replaceCount += replaceCountInTrack;
            }

            if (0 < replaceCount)
            {
                EditorUtility.SetDirty(timeline);
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"置換完了: {replaceCount} 個のクリップを置き換えました。");
        }

        private static bool ReplaceClipImpl(CriAtomTrack track, TimelineClip clip)
        {
            var currentClip = clip.asset as CriAtomClip;
            if (currentClip == null) return false;

            var newClip = track.CreateClip<CriAtomAssetClip>();
            try
            {
                // TimelineClip のパラメータコピー
                newClip.timeScale = clip.timeScale;
                newClip.start = clip.start;
                newClip.duration = clip.duration;
                newClip.clipIn = clip.clipIn;
                newClip.displayName = clip.displayName;
                newClip.easeInDuration = clip.easeInDuration;
                newClip.easeOutDuration = clip.easeOutDuration;
                newClip.blendInDuration = clip.blendInDuration;
                newClip.blendOutDuration = clip.blendOutDuration;
                newClip.blendInCurveMode = clip.blendInCurveMode;
                newClip.blendOutCurveMode = clip.blendOutCurveMode;
                newClip.mixInCurve = clip.mixInCurve;
                newClip.mixOutCurve = clip.mixOutCurve;

                var newClipAsset = newClip.asset as CriAtomAssetClip;
                using var newSo = new SerializedObject(newClipAsset);
                newSo.Update();

                newSo.FindProperty("stopWithoutRelease").boolValue = currentClip!.stopWithoutRelease;
                newSo.FindProperty("muted").boolValue = currentClip.muted;
                newSo.FindProperty("ignoreBlend").boolValue = currentClip.ignoreBlend;
                newSo.FindProperty("loopWithinClip").boolValue = currentClip.loopWithinClip;
                newSo.FindProperty("stopAtClipEnd").boolValue = currentClip.stopAtClipEnd;

                var cueReferenceProp = newSo.FindProperty("cue");
                var cueReference = GetCueReference(currentClip!.cueSheet, currentClip!.cueName);
                cueReferenceProp.FindPropertyRelative("acbAsset").objectReferenceValue = cueReference.AcbAsset;
                cueReferenceProp.FindPropertyRelative("cueId").intValue = cueReference.CueId;

                using var currentSo = new SerializedObject(currentClip);
                newSo.FindProperty("clipDuration").floatValue = currentSo.FindProperty("clipDuration").floatValue;

                var newBehaviourProp = newSo.FindProperty("templateBehaviour");
                var currentBehaviourProp = currentSo.FindProperty("templateBehaviour");
                newBehaviourProp.FindPropertyRelative("volume").floatValue = currentBehaviourProp.FindPropertyRelative("volume").floatValue;
                newBehaviourProp.FindPropertyRelative("pitch").floatValue = currentBehaviourProp.FindPropertyRelative("pitch").floatValue;
                newBehaviourProp.FindPropertyRelative("AISACValue").floatValue = currentBehaviourProp.FindPropertyRelative("AISACValue").floatValue;

                newSo.ApplyModifiedProperties();
                track.DeleteClip(clip); // コピーが終わったら古いクリップは削除
                return true;
            }
            catch (Exception e)
            {
                track.DeleteClip(newClip);
                Debug.LogError($"クリップの置き換えに失敗しました: {e.Message}");
                return false;
            }
        }

        private static CriAtomCueReference GetCueReference(string cueSheetName, string cueName)
        {
            CriAtomAcbAsset acbAsset = null;
            CriAtomExAcb acbHandle = null;
            if (!string.IsNullOrEmpty(cueSheetName))
            {
                var acbAssetKey = SoundAssetResolver.GetAcbAssetKey(cueSheetName);
                var acbAssetPath = SoundAssetResolver.KeyToAssetPath(acbAssetKey);
                acbAsset = AssetDatabase.LoadAssetAtPath<CriAtomAcbAsset>(acbAssetPath);
                acbHandle = SoundPlayRequestPreviewer.LoadAcb(cueSheetName);
            }

            var cueId = acbHandle == null ? 0 : acbHandle.GetCueInfoList().First(cueInfo => cueInfo.name == cueName).id;
            return new CriAtomCueReference(acbAsset, cueId);
        }
    }
}
