using System;
using TbfCa.SoundPlayRequest;
using UnityEditor;
using UnityEngine;

namespace TbfCa.Editor
{
    [CustomPropertyDrawer(typeof(SoundPlayRequestWithReference))]
    public class SoundPlayRequestWithReferencePropertyDrawer : PropertyDrawer, IDisposable
    {
        private SoundPlayRequestPreviewer Previewer { get; set; }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            base.GetPropertyHeight(property, label) +
            EditorGUIUtility.singleLineHeight * 2f +
            EditorGUIUtility.standardVerticalSpacing +
            Previewer?.GetPropertyDrawerHeight() ?? 0f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, label);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            var cueReferenceProperty = property.FindPropertyRelative("Reference");
            var cueSheetProperty = cueReferenceProperty.FindPropertyRelative("acbAsset");
            var cueIdProperty = cueReferenceProperty.FindPropertyRelative("cueId");

            EditorGUI.indentLevel++;
            try
            {
                var cueSheetRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(cueSheetRect, cueSheetProperty);
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                var cueIdRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(cueIdRect, cueIdProperty);
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                var cueSheetName = cueSheetProperty.objectReferenceValue?.name ?? string.Empty;
                var cueId = cueIdProperty?.intValue ?? 0;
                Previewer ??= new SoundPlayRequestPreviewer();
                Previewer.Draw(position, cueSheetName, cueId: cueId);
            }
            finally
            {
                EditorGUI.indentLevel--;
            }
        }

        public void Dispose()
        {
            Previewer?.Dispose();
        }
    }
}