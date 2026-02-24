using System;
using TbfCa.SoundPlayRequest;
using UnityEditor;
using UnityEngine;

namespace TbfCa.Editor
{
    [CustomPropertyDrawer(typeof(SoundPlayRequestWithName))]
    public class SoundPlayRequestPropertyDrawer : PropertyDrawer, IDisposable
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

            var cueSheetNameProp = property.FindPropertyRelative("CueSheetName");
            var cueNameProp = property.FindPropertyRelative("CueName");

            EditorGUI.indentLevel++;
            try
            {
                var cueSheetNameRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(cueSheetNameRect, cueSheetNameProp);
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                var cueNameRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(cueNameRect, cueNameProp);
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                Previewer ??= new SoundPlayRequestPreviewer();
                Previewer.Draw(position, cueSheetNameProp.stringValue, cueName: cueNameProp.stringValue);
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