using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TbfCa.Editor
{
    public class SoundAssetsImportWindow : EditorWindow
    {
        private string ImportDestinationFolderPath { get; set; }
        private readonly string DefaultImportFolderPath = $"{Application.dataPath}/CriAssets";

        private OrderMode CurrentOrderMode { get; set; }
        private Vector2 SelectedFileListScroll { get; set; }
        private string SelectedFolderPath { get; set; }
        private FileSelectionState[] FileSelections { get; set; } = Array.Empty<FileSelectionState>();

        private List<string> ImportResultMessages { get; } = new();
        private const string ErrorMessagePrefix = "[Error]";

        private class FileSelectionState
        {
            public bool WillImport { get; set; }
            public string FilePath { get; set; }
        }

        private enum OrderMode
        {
            LastWriteTime,
            Name,
        }

        [MenuItem("Sound/CRIファイルのインポートツール")]
        private static void Open()
        {
            GetWindow<SoundAssetsImportWindow>();
        }

        private void Awake()
        {
            ImportDestinationFolderPath = DefaultImportFolderPath;
        }

        private void OnEnable()
        {
            Clear();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("CriAtomCraft から出力されたデータを Unity にインポートします.\nACB, AWB, ACF ファイルに対応しています.", MessageType.Info);

            DrawImportDestination();

            EditorGUILayout.Space();

            DrawFilesSection();

            EditorGUILayout.Space();

            DrawImportSection();

            EditorGUILayout.Space();

            DrawResultMessage();
        }

        private void DrawImportDestination()
        {
            EditorGUILayout.LabelField("---- インポート先 ----");
            if (GUILayout.Button("インポート先を選択"))
            {
                var importFolderPath = EditorUtility.OpenFolderPanel("Open Folder", "", "");
                if (!string.IsNullOrEmpty(importFolderPath))
                {
                    if (IsUnderProjectPath(importFolderPath))
                    {
                        ImportDestinationFolderPath = importFolderPath;
                    }
                    else
                    {
                        Debug.LogError("インポート先はプロジェクト内のフォルダを指定してください. 指定されたフォルダ : " + importFolderPath);
                    }
                }
            }

            if (string.IsNullOrEmpty(ImportDestinationFolderPath))
            {
                EditorGUILayout.HelpBox("インポート先を選択してください", MessageType.Info);
            }
            else
            {
                var displayPath = ImportDestinationFolderPath.Replace(Application.dataPath, "Assets");
                EditorGUILayout.LabelField(displayPath);
            }
        }

        private void DrawFilesSection()
        {
            EditorGUILayout.LabelField("---- インポートするアセット ----");

            if (GUILayout.Button("インポートするデータが入ったフォルダを選択"))
            {
                var selectedFolderPath = EditorUtility.OpenFolderPanel("Open Folder", "", "");
                if (!string.IsNullOrEmpty(selectedFolderPath))
                {
                    if (IsUnderProjectPath(selectedFolderPath))
                    {
                        Debug.LogError("インポートするファイルはプロジェクト外のフォルダから選択してください. 選択されたフォルダ : " + selectedFolderPath);
                    }
                    else
                    {
                        SelectedFolderPath = selectedFolderPath;
                        FileSelections = Directory.GetFiles(SelectedFolderPath)
                            .Where(path => Path.GetExtension(path) is ".acb" or ".awb" or ".acf")
                            .Select(path => new FileSelectionState
                            {
                                FilePath = path,
                                WillImport = false,
                            })
                            .ToArray();
                        UpdateListOrder();
                        SelectedFileListScroll = Vector2.zero;
                    }
                }
            }

            if (string.IsNullOrEmpty(SelectedFolderPath))
            {
                EditorGUILayout.HelpBox("インポートするファイルが入ったフォルダを選択してください", MessageType.Info);
                return;
            }

            using var _ = new EditorGUI.IndentLevelScope();

            using (var orderByChange = new EditorGUI.ChangeCheckScope())
            {
                CurrentOrderMode = (OrderMode)GUILayout.SelectionGrid((int)CurrentOrderMode, new[] { "更新日時順", "名前順" }, 2);

                if (orderByChange.changed)
                {
                    UpdateListOrder();
                }
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using var scroll = new EditorGUILayout.ScrollViewScope(SelectedFileListScroll, GUILayout.Height(200f));
                SelectedFileListScroll = scroll.scrollPosition;

                using var selectedFileChangeScope = new EditorGUI.ChangeCheckScope();
                foreach (var fileSelection in FileSelections)
                {
                    using var __ = new EditorGUILayout.HorizontalScope();
                    var displayFileName = fileSelection.FilePath.Replace(SelectedFolderPath, string.Empty);
                    fileSelection.WillImport = EditorGUILayout.ToggleLeft(displayFileName, fileSelection.WillImport);
                }
            }
            return;

            void UpdateListOrder()
            {
                FileSelections = CurrentOrderMode switch
                {
                    OrderMode.LastWriteTime => FileSelections
                        .OrderByDescending(fileSelection => new FileInfo(fileSelection.FilePath).LastWriteTime)
                        .ToArray(),
                    OrderMode.Name => FileSelections.OrderBy(fileSelection => fileSelection.FilePath).ToArray(),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }

        private void DrawImportSection()
        {
            using var _ = new EditorGUI.DisabledScope(disabled: FileSelections.Length == 0);

            if (GUILayout.Button("インポート"))
            {
                ImportResultMessages.Clear();
                if (!Directory.Exists(ImportDestinationFolderPath))
                {
                    ImportResultMessages.Add(ErrorMessagePrefix + "インポート先のフォルダが見つかりませんでした : " + ImportDestinationFolderPath);
                    return;
                }

                var selectedFiles = FileSelections
                    .Where(fileSelection => fileSelection.WillImport)
                    .Select(fileSelection => fileSelection.FilePath)
                    .ToArray();
                foreach (var filePath in selectedFiles)
                {
                    ImportFile(filePath);
                }

                Clear();
            }
        }

        private void ImportFile(string sourcePath)
        {
            if (!File.Exists(sourcePath))
            {
                ImportResultMessages.Add(ErrorMessagePrefix + "インポートしようとしたファイルが見つかりませんでした : " + sourcePath);
                return;
            }

            var basePath = Path.GetRelativePath(relativeTo: Application.dataPath, path: ImportDestinationFolderPath);
            var importTo = $"Assets/{basePath}/{Path.GetFileName(sourcePath)}";
            var isOverwritten = File.Exists(importTo);
            File.Copy(sourcePath, importTo, overwrite: true);
            AssetDatabase.ImportAsset(importTo, ImportAssetOptions.ForceUpdate);
            var prefix = isOverwritten ? "[上書き]" : "[追加]";
            ImportResultMessages.Add(prefix + "ファイルをインポートしました : " + importTo);
        }

        private void DrawResultMessage()
        {
            if (ImportResultMessages.Count == 0)
                return;

            EditorGUILayout.LabelField("インポート結果");
            var originalColor = GUI.color;
            foreach (var resultMessage in ImportResultMessages)
            {
                var textColor = resultMessage.StartsWith(ErrorMessagePrefix) ? Color.red : Color.white;
                GUI.color = textColor;
                GUILayout.Box(resultMessage, GUILayout.ExpandWidth(true));
            }
            GUI.color = originalColor;
        }

        private void Clear()
        {
            FileSelections = Array.Empty<FileSelectionState>();
            SelectedFileListScroll = Vector2.zero;
            SelectedFolderPath = string.Empty;
        }

        private static bool IsUnderProjectPath(string path)
        {
            var fullPath = Path.GetFullPath(path).Replace('\\', '/');
            var projectRoot = Application.dataPath;
            return fullPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase);
        }
    }
}