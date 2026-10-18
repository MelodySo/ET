#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using WalldoffStudios.Extensions;
using WalldoffStudios.Indicators;

[CustomEditor(typeof(IndicatorController))]
[CanEditMultipleObjects]
public class IndicatorControllerEditor : Editor
{
    private IndicatorController indicatorController;
    private IndicatorBase indicator;
    private IndicatorSettings indicatorSettings;

    private SerializedProperty indicatorInfos;
    private SerializedProperty indicatorVisuals;

    private Texture2D coneIcon;
    private Texture2D parabolicIcon;
    private Texture2D lineIcon;
    private Texture2D targetIcon;
    private Texture2D currentIcon;

    private Texture2D duplicationIcon;
    private Texture2D deleteIcon;
    
    private Material[] indicatorMats = new Material[4];

    private readonly Color inspectorBackground = new Color(0.07f, 0.16f, 0.2f);

    private string folderName = "AimIndicators";
    private string folderPath;

    private void Awake()
    {
        string[] guids = AssetDatabase.FindAssets("t:Folder " + folderName); // Search for all folders with the given name
        if (guids.Length > 0)
        {
            folderPath = AssetDatabase.GUIDToAssetPath(guids[0]); // Get the path of the first folder found
            if (guids.Length > 1)
            {
                Debug.LogWarning($"Multiple folders are named {folderName}, this can prevent automatic functions to be set correctly");
            }
        }
        else
        {
            Debug.LogWarning("Could not find folder with name: " + folderName);
        }
    }

    private void OnEnable()
    {
        indicatorInfos = serializedObject.FindProperty("indicatorInfos");
        indicatorVisuals = serializedObject.FindProperty("indicatorVisuals");
        
        indicatorController = target as IndicatorController;

        if (indicatorController != null)
        {
            indicatorController.CollectChildIndicators();   
        }

        if (Application.isPlaying == false)
        {
            CollectMaterials();   
        }
        CollectIconTextures();
    }

    private void CollectMaterials()
    {
        string[] guids = AssetDatabase.FindAssets("t:material", new string[] { folderPath });
        IEnumerable<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath);
        
        indicatorMats = paths.Select(AssetDatabase.LoadAssetAtPath<Material>).ToArray();
        indicatorController.SetMaterialReferences(indicatorMats);
    }
    private void CollectIconTextures()
    {
        coneIcon = (Texture2D)AssetDatabase.LoadAssetAtPath($"{folderPath}/Textures/Icons/coneIcon.png", typeof(Texture2D));
        parabolicIcon = (Texture2D)AssetDatabase.LoadAssetAtPath($"{folderPath}/Textures/Icons/parabolicIcon.png", typeof(Texture2D));
        lineIcon = (Texture2D)AssetDatabase.LoadAssetAtPath($"{folderPath}/Textures/Icons/lineIcon.png", typeof(Texture2D));
        targetIcon = (Texture2D)AssetDatabase.LoadAssetAtPath($"{folderPath}/Textures/Icons/targetIcon.png", typeof(Texture2D));
        
        duplicationIcon = (Texture2D)AssetDatabase.LoadAssetAtPath($"{folderPath}/Textures/Icons/duplicateIcon.png", typeof(Texture2D));
        deleteIcon = (Texture2D)AssetDatabase.LoadAssetAtPath($"{folderPath}/Textures/Icons/deleteIcon.png", typeof(Texture2D));
    }

    public override void OnInspectorGUI()
    {
        indicatorController = target as IndicatorController;
        if (indicatorController == null) return;
        
        serializedObject.Update();
        
        var screenRect = GUILayoutUtility.GetRect(1, 1);
        var vertRect = EditorGUILayout.BeginVertical();
        EditorGUI.DrawRect(new Rect(screenRect.x - 18, screenRect.y -5, screenRect.width + 25, vertRect.height + 18), inspectorBackground);

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.Space(10);
        
        Show(indicatorInfos);
        
        EditorGUILayout.Space(25);
        
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.PropertyField(indicatorVisuals, new GUIContent("Visuals"));   
        }

        EditorGUILayout.Space(5);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(new GUIContent("Add indicator", "adds a new cone indicator with standard settings")))
            {
                indicatorController.AddIndicator();
                EditorUtility.SetDirty(target);
            }

            if (GUILayout.Button(new GUIContent("Remove all", "Deletes all indicators from this list and the gameObjects from the hierarchy")))
            {
                indicatorController.transform.ClearChildren();
                indicatorInfos.ClearArray();
            }      
        }
        
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            if (Application.isPlaying == false)
            {
                indicatorController.SetCorrectIndicatorType();   
            }
        }
        EditorGUILayout.EndVertical();
    }
    
    private void Show(SerializedProperty array)
    {
        var rect = GUILayoutUtility.GetRect(1, 1);
        if (array.arraySize != indicatorController.transform.childCount)
        {
            indicatorController.ChildTransformDeleted();
        }
        for (int i = 0; i < array.arraySize; i++)
        {
            var vertRect = EditorGUILayout.BeginVertical();
            GUI.Box(new Rect(rect.x,vertRect.y -2.5f, 79.0f, 79.0f ), GetIndicatorIcon(indicatorController.GetCurrentType(i)));
            using (new EditorGUILayout.VerticalScope(IndicatorStyle()))
            {
                EditorGUILayout.Space(1.0f);
                var prop = array.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(prop);
                if (GUI.Button(new Rect(rect.x + rect.width - 30.0f, vertRect.y + 5.0f,22.0f,rect.y + 5.0f), new GUIContent("", "Remove indicator"), RemoveStyle()))
                {
                    indicatorController.RemoveIndicator(i);
                    prop.DeleteCommand();
                }
                if (GUI.Button(new Rect(rect.x + rect.width - 60.0f, vertRect.y + 2.0f,25.0f,rect.y + 10.0f), new GUIContent("", "Duplicate indicator"), DuplicateStyle()))
                {
                    indicatorController.DuplicateIndicator(i);
                    EditorUtility.SetDirty(target);
                }
                EditorGUILayout.Space(45.0f);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.Space(7.0f);
        }
    }

    private Texture2D GetIndicatorIcon(IndicatorType type)
    {
        switch (type)
        {
            case IndicatorType.CONE:
                currentIcon = coneIcon;
                break;
            case IndicatorType.LINE:
                currentIcon = lineIcon;
                break;
            case IndicatorType.PARABOLIC:
                currentIcon = parabolicIcon;
                break;
            case IndicatorType.TARGET:
                currentIcon = targetIcon;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        return currentIcon;
    }
    
    private GUIStyle IndicatorStyle()
    {
        var style = EditorStyles.helpBox;
        style.alignment = TextAnchor.MiddleRight;
        style.margin = new RectOffset(82, 0, 0, 0);
        
        return style;
    }

    private GUIStyle DuplicateStyle()
    {
        var style = new GUIStyle
        {
            normal =
            {
                background = duplicationIcon
            },
            alignment = TextAnchor.MiddleCenter
        };

        return style;
    }
    
    private GUIStyle RemoveStyle()
    {
        var style = new GUIStyle
        {
            normal =
            {
                background = deleteIcon
            },
            alignment = TextAnchor.MiddleCenter
        };

        return style;
    }
}   
#endif