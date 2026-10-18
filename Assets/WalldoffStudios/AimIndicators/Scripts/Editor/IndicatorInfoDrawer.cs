#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using WalldoffStudios.Indicators;

[CustomPropertyDrawer(typeof(IndicatorInfo))]
public class IndicatorInfoDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        using (new EditorGUI.PropertyScope(position, label, property))
        {
            // Calculate rects
            var typeRect = new Rect(position.x + 2, position.y, 90, EditorGUIUtility.singleLineHeight);
            var indicatorRect = new Rect(position.x +2, position.y + EditorGUIUtility.singleLineHeight + 5, position.width -5, EditorGUIUtility.singleLineHeight);
            var settingsRect = new Rect(position.x +2, position.y + EditorGUIUtility.singleLineHeight * 2 + 7.5f, position.width - 5, EditorGUIUtility.singleLineHeight);

            if (Application.isPlaying == false)
            {
                EditorGUI.PropertyField(typeRect, property.FindPropertyRelative("type"), GUIContent.none);   
            }
            EditorGUI.PropertyField(indicatorRect, property.FindPropertyRelative("indicator"), GUIContent.none);
            EditorGUI.PropertyField(settingsRect, property.FindPropertyRelative("settings"), GUIContent.none);
        }
    }
}
#endif