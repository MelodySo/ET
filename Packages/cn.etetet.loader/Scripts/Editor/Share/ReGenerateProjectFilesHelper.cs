using UnityEditor;
using UnityEngine;

namespace ET
{
    public static class ReGenerateProjectFilesHelper
    {
        [MenuItem("ET/Loader/ReGenerateVisualStudioProjectFiles")]
        public static void RegenerateVisualStudio()
        {
            // 插件同时发现 VS Code；此菜单只选择真正的 Visual Studio，修复 internal 编辑器空操作。
            System.Type editorType = System.Type.GetType("Microsoft.Unity.VisualStudio.Editor.VisualStudioEditor, Unity.VisualStudio.Editor");
            if (editorType == null)
                throw new System.InvalidOperationException("未找到 Visual Studio Editor 插件。");
            var editor = (Unity.CodeEditor.IExternalCodeEditor)System.Activator.CreateInstance(editorType);
            string selected = null;
            foreach (var installation in editor.Installations)
            {
                if (!string.Equals(System.IO.Path.GetFileName(installation.Path), "devenv.exe", System.StringComparison.OrdinalIgnoreCase)) continue;
                if (!System.IO.File.Exists(installation.Path)) continue;
                selected = installation.Path;
                if (string.Equals(selected, Unity.CodeEditor.CodeEditor.CurrentEditorInstallation, System.StringComparison.OrdinalIgnoreCase)) break;
            }
            if (selected == null)
                throw new System.InvalidOperationException("未检测到 Visual Studio，请在 External Tools 中选择已安装的 Visual Studio。");
            Unity.CodeEditor.CodeEditor.SetExternalScriptEditor(selected);
            Unity.CodeEditor.CodeEditor.CurrentEditor.SyncAll();
            Debug.Log($"已使用 Visual Studio 刷新 ET 工程：{selected}");
        }

        [InitializeOnLoadMethod]
        [MenuItem("ET/Loader/ReGenerateProjectFiles")]
        public static void Run()
        {
            Unity.CodeEditor.CodeEditor.CurrentEditor.SyncAll();

            foreach (string s in FileHelper.GetAllFiles(".", "Ignore.ET*.csproj"))
            {
                System.IO.File.Delete(s);
            }

            Debug.Log("regenerate csproj");
        }
    }
}
