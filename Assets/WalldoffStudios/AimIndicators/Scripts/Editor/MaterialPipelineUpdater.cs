#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace WalldoffStudios.Utils
{
    public static class MaterialPipelineUpdater
    {
        private static string folderName = "AimIndicators";
        private static string folderPath;

        private static void GetFolderPath()
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
        
        [MenuItem("WalldoffStudios/Set scene materials pipeline/Built in")]
        static void SetBuiltInPipeline()
        {
            if (string.IsNullOrEmpty(folderPath)) GetFolderPath();
            
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { $"{folderPath}/Materials/SceneItems" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = (Material)AssetDatabase.LoadAssetAtPath(path, typeof(Material));
                
                material.shader = Shader.Find("Standard"); 
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
        }
        
        [MenuItem("WalldoffStudios/Set scene materials pipeline/URP")]
        static void SetUniversalPipeline()
        {
            if (string.IsNullOrEmpty(folderPath)) GetFolderPath();
            
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { $"{folderPath}/Materials/SceneItems" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = (Material)AssetDatabase.LoadAssetAtPath(path, typeof(Material));
                
                material.shader = Shader.Find("Universal Render Pipeline/Lit"); 
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
        }
        
        [MenuItem("WalldoffStudios/Set scene materials pipeline/HDRP")]
        static void SetHighDefinitionPipeline()
        {
            if (string.IsNullOrEmpty(folderPath)) GetFolderPath();
            
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { $"{folderPath}/Materials/SceneItems" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = (Material)AssetDatabase.LoadAssetAtPath(path, typeof(Material));
                
                material.shader = Shader.Find("HDRP/Lit"); 
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
        }
    }   
}
#endif
