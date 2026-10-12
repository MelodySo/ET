using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ET.Test
{
    public class Brawl_LevelEditorState_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;
            string directory = Path.GetFullPath(Path.Combine("Temp", "BrawlLevelTests", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "BrawlLevel.xlsx");
            File.Copy(BrawlLevelWorkbook.DefaultPath, path);
            BrawlLevelEditorWindow window = ScriptableObject.CreateInstance<BrawlLevelEditorWindow>();
            BrawlLevelEditorWindow restored = null;
            try
            {
                int id = 2000000010;
                if (window.SelectedLevelId != 0) return Fail(1, "Initial selection is not empty.");
                window.CreateLevel(id, path);
                if (window.SelectedLevelId != id || !window.hasUnsavedChanges) return Fail(2, "New draft state is incorrect.");
                using (SerializedObject state = new(window))
                {
                    state.FindProperty("modeId").intValue = 1;
                    state.FindProperty("sceneAsset").stringValue = "LevelStateTest";
                    state.FindProperty("rowCount").intValue = 3;
                    state.FindProperty("columnCount").intValue = 4;
                    state.ApplyModifiedPropertiesWithoutUndo();
                }
                window.SaveLevel(path);
                window.SaveLevel(path);
                if (window.SelectedLevelId != id || window.hasUnsavedChanges) return Fail(3, "Save cleared selection or left dirty state.");
                window.ExitLevel();
                if (window.SelectedLevelId != 0) return Fail(4, "Exit did not clear selection.");
                window.ImportLevel(id, path);
                if (window.SelectedLevelId != id || window.hasUnsavedChanges) return Fail(5, "Import state is incorrect.");
                restored = ScriptableObject.CreateInstance<BrawlLevelEditorWindow>();
                EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(window), restored);
                if (restored.SelectedLevelId != id) return Fail(6, "Serialized editor state lost selection.");
                using (SerializedObject state = new(restored))
                {
                    if (state.FindProperty("rowCount").intValue != 3 || state.FindProperty("columnCount").intValue != 4 ||
                        state.FindProperty("sceneAsset").stringValue != "LevelStateTest")
                        return Fail(7, "Saved/imported draft properties were lost.");
                    state.FindProperty("sceneAsset").stringValue = "UnsavedChange";
                    state.ApplyModifiedPropertiesWithoutUndo();
                }
                restored.ExitLevel();
                BrawlLevelConfig saved = BrawlLevelWorkbook.Read(path).Load(id, out _);
                if (saved.SceneAsset != "LevelStateTest") return Fail(8, "Exit unexpectedly saved changes.");
                return ErrorCode.ERR_Success;
            }
            finally
            {
                window.DiscardChanges();
                UnityEngine.Object.DestroyImmediate(window);
                if (restored != null) { restored.DiscardChanges(); UnityEngine.Object.DestroyImmediate(restored); }
                if (File.Exists(path)) File.Delete(path);
                Directory.Delete(directory);
            }
        }

        private static int Fail(int code, string message)
        {
            Log.Console($"Brawl editor state test failed: {message}");
            return 55100 + code;
        }
    }
}
