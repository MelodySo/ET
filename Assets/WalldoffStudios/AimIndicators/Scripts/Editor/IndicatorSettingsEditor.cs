#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using WalldoffStudios.Indicators;

[CustomEditor(typeof(IndicatorSettings))]
[CanEditMultipleObjects]
public class IndicatorSettingsEditor : Editor
{
    private IndicatorSettings indicatorSettings;
    private IndicatorBase indicator;

    private SerializedProperty alwaysDisplayIndicator;
    private SerializedProperty endPointTarget;
    private SerializedProperty mainTex;
    private SerializedProperty renderEdges;
    private SerializedProperty edgeTex;
    private SerializedProperty range;
    private SerializedProperty edgePadding;
    private SerializedProperty radialSize;
    private SerializedProperty fov;
    private SerializedProperty raycasts;
    private SerializedProperty timeBetweenRaycasts;
    private SerializedProperty minDistanceForUpdate;
    private SerializedProperty lerpTime;
    private SerializedProperty obstacleMask;
    private SerializedProperty useHitDetection;
    private SerializedProperty drawDebug;
    private SerializedProperty height;
    private SerializedProperty meshWidth;
    private SerializedProperty distortion;
    private SerializedProperty offset;
    private SerializedProperty resolution;
    private SerializedProperty brightness;
    private SerializedProperty mainColor;
    private SerializedProperty useFillEffect;
    private SerializedProperty fillColor;
    private SerializedProperty fillSpeed;

    private readonly Color inspectorBackground = new Color(0.07f, 0.16f, 0.2f);

    private void OnEnable()
    {
        alwaysDisplayIndicator = serializedObject.FindProperty("alwaysDisplayIndicator");
        endPointTarget = serializedObject.FindProperty("endPointTarget");
        mainTex = serializedObject.FindProperty("mainTex");
        renderEdges = serializedObject.FindProperty("renderEdges");
        edgeTex = serializedObject.FindProperty("edgeTex");
        range = serializedObject.FindProperty("range");
        edgePadding = serializedObject.FindProperty("edgePadding");
        radialSize = serializedObject.FindProperty("radialSize");
        fov = serializedObject.FindProperty("fov");
        raycasts = serializedObject.FindProperty("raycasts");
        timeBetweenRaycasts = serializedObject.FindProperty("timeBetweenRaycasts");
        minDistanceForUpdate = serializedObject.FindProperty("minDistanceForUpdate");
        lerpTime = serializedObject.FindProperty("lerpTime");
        obstacleMask = serializedObject.FindProperty("obstacleMask");
        useHitDetection = serializedObject.FindProperty("useHitDetection");
        drawDebug = serializedObject.FindProperty("drawDebug");
        height = serializedObject.FindProperty("height");
        meshWidth = serializedObject.FindProperty("meshWidth");
        resolution = serializedObject.FindProperty("resolution");
        distortion = serializedObject.FindProperty("distortion");
        offset = serializedObject.FindProperty("offset");
        brightness = serializedObject.FindProperty("brightness");
        mainColor = serializedObject.FindProperty("mainColor");
        useFillEffect = serializedObject.FindProperty("useFillEffect");
        fillColor = serializedObject.FindProperty("fillColor");
        fillSpeed = serializedObject.FindProperty("fillSpeed");
    }

    public override void OnInspectorGUI()
    {
        indicatorSettings = target as IndicatorSettings;
        if (indicatorSettings == null) return;
        if (indicator == null) indicator = indicatorSettings.gameObject.GetComponent<IndicatorBase>();

        serializedObject.Update();

        var screenRect = GUILayoutUtility.GetRect(1, 1);
        var vertRect = EditorGUILayout.BeginVertical();
        EditorGUI.DrawRect(new Rect(screenRect.x - 18, screenRect.y - 4, screenRect.width + 25, vertRect.height + 25), inspectorBackground);
        EditorGUI.BeginChangeCheck();

        switch (indicatorSettings.IndicatorType)
        {
            case IndicatorType.CONE:
                DrawConeSettings();
                break;
            case IndicatorType.LINE:
                DrawLineSettings();
                break;
            case IndicatorType.PARABOLIC:
                DrawParabolicSettings();
                break;
            case IndicatorType.TARGET:
                DrawTargetSettings();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (EditorGUI.EndChangeCheck() == true)
        {
            serializedObject.ApplyModifiedProperties();
            if (Application.isPlaying == true)
            {
                indicator.OnValuesUpdated();
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawConeSettings()
    {
        EditorGUILayout.Space(1);
        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(alwaysDisplayIndicator,
                new GUIContent("Always display indicator",
                    "Whether or not to disable the indicator when you stop aiming"));
            EditorGUILayout.PropertyField(mainTex, new GUIContent("Texture"));
            EditorGUILayout.PropertyField(renderEdges,
                new GUIContent("Render edges?", "Uncheck it it you plan to have high fov values"));
            if (indicatorSettings.RenderEdges == true)
            {
                EditorGUILayout.PropertyField(edgeTex, new GUIContent("Edge texture"));
            }
            EditorGUILayout.Space(2);
        }
        EditorGUILayout.Space(1);

        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.Slider(range, 0.1f, 100.0f, new GUIContent("Range", "How far the line reaches"));

            if (Application.isPlaying == false)
            {
                EditorGUILayout.Slider(fov, 1.0f, 360.0f, new GUIContent("FOV", "How big the cone's field of view is"));

                EditorGUILayout.IntSlider(raycasts, 3, 360, new GUIContent(
                    "Raycasts",
                    "How many interaction points we'll use, lower values will look less smooth but be more performant"));
            }

            EditorGUILayout.Slider(timeBetweenRaycasts, 0.0f, 2.0f, new GUIContent(
                "Time between raycasts", "Higher values will look less smooth but be more performant"));
            
            EditorGUILayout.Space(2);
        }
        
        EditorGUILayout.Space(1);

        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(obstacleMask,
                new GUIContent("Obstacle mask", "Layers that indicator will interact with"));

            EditorGUILayout.PropertyField(useHitDetection, new GUIContent(
                "Use Hit detection", "Enable if you want to use hit detection for generated mesh"));

            EditorGUILayout.PropertyField(drawDebug, new GUIContent(
                "Draw debug", "Visualize the raycasts in the scene view"));

            EditorGUILayout.Space(2);   
        }
        
        EditorGUILayout.Space(1);

        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.Space(2);
            
            EditorGUILayout.Slider(distortion, 0.0f, 0.2f,
                new GUIContent("Distortion", "Adjust this to fine tune distortion amount of indicator"));

            EditorGUILayout.Slider(offset, -1.0f, 1.0f, 
                new GUIContent("Offset", "Affects how close the indicator will come to the obstacle"));
            
            EditorGUILayout.Space(2);   
        }
        
        EditorGUILayout.Space(1);

        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.Slider(brightness, 0.0f, 2.0f,
                new GUIContent("Brightness", "Adjust how bright the indicator is"));

            EditorGUILayout.PropertyField(mainColor, new GUIContent("Main color"));
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.Space(1);
        
        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(useFillEffect,
                new GUIContent("Enable fill effect?", "Check if you want to enable fill effects"));
            if (indicatorSettings.UseFillEffect == true)
            {
                EditorGUILayout.PropertyField(fillColor, new GUIContent("Fill color"));
                EditorGUILayout.Slider(fillSpeed, 0.1f, 5.0f,
                    new GUIContent("Fill speed", "How long it takes for fill effect to finish"));
            }                
            EditorGUILayout.Space(2);
        }
    }

    private void DrawLineSettings()
    {
        EditorGUILayout.Space(1);
        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(alwaysDisplayIndicator,
                new GUIContent("Always display indicator",
                    "Whether or not to disable the indicator when you stop aiming"));
            EditorGUILayout.PropertyField(mainTex, new GUIContent("Texture"));
            EditorGUILayout.Space(2);
        }
        
        EditorGUILayout.Space(1);

        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.Slider(range, 0.1f, 80.0f, new GUIContent("Range", "How far the line reaches"));
            EditorGUILayout.Slider(meshWidth, 0.1f, 10.0f, new GUIContent("Width", "How wide line will be"));
            EditorGUILayout.Slider(edgePadding, 0.0f, 2.0f, new GUIContent(
                "Edge padding", "How far in from the edges we start checking with raycasts"));
            EditorGUILayout.Space(2);
        }
        
        EditorGUILayout.Space(1);

        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.Space(2);
            
            EditorGUILayout.IntSlider(raycasts, 3, 300, new GUIContent(
                "Raycasts",
                "How many interaction points we'll use, lower values will look less smooth but be more performant"));  

            EditorGUILayout.Slider(timeBetweenRaycasts, 0.0f, 2.0f, new GUIContent(
                "Time between raycasts", "Higher values will look less smooth but be more performant"));
            
            EditorGUILayout.Slider(minDistanceForUpdate, 0.0f, 1.0f, new GUIContent(
                "Min distance for update", "How far away the new hitPoint have to be in order to update the shape.\n" +
                                           "If at 0 it will update constantly even if hitPoints are the same"));

            EditorGUILayout.Space(2);
        }
        
        EditorGUILayout.Space(1);

        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(obstacleMask,
                new GUIContent("Obstacle mask", "Layers that indicator will interact with"));

            EditorGUILayout.PropertyField(useHitDetection, new GUIContent(
                "Use Hit detection", "Enable if you want to use hit detection for generated mesh"));

            EditorGUILayout.PropertyField(drawDebug, new GUIContent(
                "Draw debug", "Visualize the raycasts in the scene view"));

            EditorGUILayout.Space(2);
        }
        
        EditorGUILayout.Space(1);

        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.Space(2);
            
            EditorGUILayout.Slider(offset, 0.0f, 1.0f, new GUIContent(
                "Offset", "How far from the obstacles it will render the mesh"));
            
            EditorGUILayout.Slider(lerpTime, 0.01f, 1.0f, new GUIContent(
                "Lerp time", "How fast the mesh will adjust to new hit point, lower values will be more smooth"));
            
            EditorGUILayout.Space(2);
        }
        
        EditorGUILayout.Space(1);

        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.Slider(brightness, 0.0f, 2.0f,
                new GUIContent("Brightness", "Adjust how bright the indicator is"));

            EditorGUILayout.PropertyField(mainColor, new GUIContent("Main color"));
            EditorGUILayout.Space(2);
        }
        
        EditorGUILayout.Space(1);

        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(useFillEffect,
                new GUIContent("Enable fill effect?", "Check if you want to enable fill effects"));
            if (indicatorSettings.UseFillEffect == true)
            {
                EditorGUILayout.PropertyField(fillColor, new GUIContent("Fill color"));
                EditorGUILayout.Slider(fillSpeed, 0.1f, 5.0f,
                    new GUIContent("Fill speed", "How long it takes for fill effect to finish"));
            }
            EditorGUILayout.Space(2);
        }
        EditorGUILayout.Space(1);
    }

    private void DrawParabolicSettings()
    {
        EditorGUILayout.Space(1);
        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(alwaysDisplayIndicator,
                new GUIContent("Always display indicator",
                    "Whether or not to disable the indicator when you stop aiming"));
            EditorGUILayout.PropertyField(endPointTarget, new GUIContent("Target"));
            EditorGUILayout.PropertyField(mainTex, new GUIContent("Texture"));
            EditorGUILayout.Space(2);   
        }

        EditorGUILayout.Space(1);
        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.Slider(range, 0.1f, 100.0f, new GUIContent("Range", "How far the line reaches"));
            EditorGUILayout.Slider(meshWidth, 0.1f, 10.0f,
                new GUIContent("Width", "How wide the parabola curve will be"));

            if (Application.isPlaying == false)
            {
                EditorGUILayout.Slider(height, 5.0f, 80.0f,
                    new GUIContent("Height", "Max height of the parabolic curve"));
                EditorGUILayout.Slider(resolution, 0.02f, 0.3f,
                    new GUIContent("Resolution",
                        "Lower values will make it more smooth but be less performant. \n" +
                        "Some values will cause end of parabola to look weird, keep changing this value by a small amount until it's fixed."));
            }
            EditorGUILayout.Space(2);
        }
        
        EditorGUILayout.Space(1);

        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.Slider(brightness, 0.0f, 2.0f,
                new GUIContent("Brightness", "Adjust how bright the indicator is"));

            EditorGUILayout.PropertyField(mainColor, new GUIContent("Main color"));
            EditorGUILayout.Space(2);
        }
        
        EditorGUILayout.Space(1);
        
        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(useFillEffect,
                new GUIContent("Enable fill effect?", "Check if you want to enable fill effects"));
            if (indicatorSettings.UseFillEffect == true)
            {
                EditorGUILayout.PropertyField(fillColor, new GUIContent("Fill color"));
                EditorGUILayout.Slider(fillSpeed, 0.1f, 5.0f,
                    new GUIContent("Fill speed", "How long it takes for fill effect to finish"));
            }   
            EditorGUILayout.Space(2);
        }
        EditorGUILayout.Space(1);
    }

    private void DrawTargetSettings()
    {
        
        EditorGUILayout.Space(1);
        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(alwaysDisplayIndicator,
                new GUIContent("Always display indicator",
                    "Whether or not to disable the indicator when you stop aiming"));
            EditorGUILayout.PropertyField(mainTex, new GUIContent("Texture"));
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.Space(1);
        
        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.Space(2);
            
            EditorGUILayout.Slider(radialSize, 1.0f, 100.0f, new GUIContent(
                "Radial size", "Radial size of target"));
            
            EditorGUILayout.Slider(lerpTime, 0.01f, 1.0f, new GUIContent(
                "Lerp time", "How fast the mesh will adjust to new radial size, lower values will be more smooth"));
            
            EditorGUILayout.Space(2);
        }
        
        EditorGUILayout.Space(1);

        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.Slider(brightness, 0.0f, 2.0f,
                new GUIContent("Brightness", "Adjust how bright the indicator is"));

            EditorGUILayout.PropertyField(mainColor, new GUIContent("Main color"));
            EditorGUILayout.Space(2);
        }
        
        EditorGUILayout.Space(1);

        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(useFillEffect,
                new GUIContent("Enable fill effect?", "Check if you want to enable fill effects"));
            if (indicatorSettings.UseFillEffect == true)
            {
                EditorGUILayout.PropertyField(fillColor, new GUIContent("Fill color"));
                EditorGUILayout.Slider(fillSpeed, 0.1f, 5.0f,
                    new GUIContent("Fill speed", "How long it takes for fill effect to finish"));
            }    
            EditorGUILayout.Space(2);
        }
        EditorGUILayout.Space(1);
    }
}
#endif