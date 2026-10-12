using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using MongoDB.Bson;

namespace ET
{
    /// <summary>复用仓库 .NET 工具；大数组通过 stdin 传递，不受命令行参数长度限制。</summary>
    internal static class BrawlEditorToolRunner
    {
        public static string Excel(string tool, BsonDocument arguments)
        {
            BsonDocument request = new()
            {
                { "jsonrpc", "2.0" }, { "id", 1 }, { "method", "tools/call" },
                { "params", new BsonDocument { { "name", tool }, { "arguments", arguments } } },
            };
            string output = Run("ET.ExcelMcp.dll", request.ToJson());
            BsonDocument response = BsonDocument.Parse(output.Trim());
            if (response.TryGetValue("error", out BsonValue error))
                throw new IOException($"Excel 工具失败：{error}");
            BsonDocument result = response["result"].AsBsonDocument;
            if (result.GetValue("isError", false).ToBoolean())
                throw new IOException($"Excel 工具失败：{result}");
            return result["content"][0]["text"].AsString;
        }

        public static string Export()
        {
            return Run("ET.ExcelExporter.dll", null);
        }

        private static string Run(string dllName, string input)
        {
            string root = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
            string dll = Path.Combine(root, "Bin", dllName);
            if (!File.Exists(dll)) throw new FileNotFoundException("工具未编译，请先执行 dotnet build ET.sln。", dll);
            using Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet", Arguments = $"\"{dll}\"", WorkingDirectory = root,
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8,
                    StandardInputEncoding = new UTF8Encoding(false),
                },
            };
            process.Start();
            // Concurrent draining prevents large tool output from blocking a child process.
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            try
            {
                if (input != null) process.StandardInput.WriteLine(input);
                process.StandardInput.Close();
                if (!process.WaitForExit(60000))
                {
                    process.Kill();
                    process.WaitForExit();
                    throw new TimeoutException($"{dllName} 执行超时，工作簿未提交。请检查日志后重试。");
                }
                string output = stdout.GetAwaiter().GetResult();
                string errors = stderr.GetAwaiter().GetResult();
                if (process.ExitCode != 0) throw new IOException($"{dllName} 失败（{process.ExitCode}）：\n{errors}\n{output}");
                return output;
            }
            finally
            {
                if (!process.HasExited) { process.Kill(); process.WaitForExit(); }
            }
        }
    }
}
