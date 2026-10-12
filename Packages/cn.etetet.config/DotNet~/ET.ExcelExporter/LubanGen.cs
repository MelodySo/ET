using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace ET
{
    public class LubanInfo
    {
        public string LubanConfigPath;

        public List<string> subConfigDir = new List<string>();
    }

    public static class LubanGen
    {
        private class SchemaFile
        {
            public string fileName { get; set; }
            public string type { get; set; }
        }

        private static Dictionary<string, LubanInfo> lubanInfos = new();

        public static bool CreateLubanConf(string configType)
        {
            lubanInfos.Clear();
            foreach (string packageDir in Directory.GetDirectories("Packages", "cn.etetet.*"))
            {
                string d = Path.Combine(packageDir, "Luban");
                if (!Directory.Exists(d))
                {
                    continue;
                }

                foreach (string directory in Directory.GetDirectories(d))
                {
                    string configCollectionName = Path.GetFileName(directory);
                    if (!lubanInfos.TryGetValue(configCollectionName, out LubanInfo lubanInfo))
                    {
                        lubanInfo = new LubanInfo();
                        lubanInfos.Add(configCollectionName, lubanInfo);
                    }

                    if (File.Exists(Path.Combine(directory, "luban.conf")))
                    {
                        lubanInfo.LubanConfigPath = directory;
                    }

                    lubanInfo.subConfigDir.Add(directory);
                }
            }

            foreach ((string configCollectionsName, LubanInfo lubanInfo) in lubanInfos)
            {
                List<SchemaFile> AllSchemaFile = new();
                foreach (var configPath in lubanInfo.subConfigDir)
                {
                    var tablesPath = Path.Combine(configPath, "Base/__tables__.xlsx");
                    if (File.Exists(tablesPath))
                    {
                        AllSchemaFile.Add(new() { fileName = $"../../../../{tablesPath}", type = "table" });
                    }

                    var beansPath = Path.Combine(configPath, "Base/__beans__.xlsx");
                    if (File.Exists(beansPath))
                    {
                        AllSchemaFile.Add(new() { fileName = $"../../../../{beansPath}", type = "bean" });
                    }

                    var enumsPath = Path.Combine(configPath, "Base/__enums__.xlsx");
                    if (File.Exists(enumsPath))
                    {
                        AllSchemaFile.Add(new() { fileName = $"../../../../{enumsPath}", type = "enum" });
                    }

                    var definesPath = Path.Combine(configPath, "Defines");
                    if (Directory.Exists(definesPath))
                    {
                        AllSchemaFile.Add(new() { fileName = $"../../../../{definesPath}", type = "" });
                    }
                }

                var packagePath = $"{lubanInfo.LubanConfigPath}/luban.conf";

                if (!File.Exists(packagePath))
                {
                    Console.WriteLine($"[ERROR] 源文件 luban.conf 不存在 请重新初始化LubanGen");
                    Console.Out.Flush();
                    return false;
                }

                try
                {
                    string fileContent = File.ReadAllText(packagePath);

                    var json = JsonDocument.Parse(fileContent).RootElement;
                    var jsonDict = JsonSerializer.Deserialize<Dictionary<string, object>>(fileContent);

                    jsonDict["schemaFiles"] = AllSchemaFile;

                    string updatedJsonContent = JsonSerializer.Serialize(jsonDict, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                    File.WriteAllText(packagePath, updatedJsonContent, Encoding.UTF8);

                    Console.WriteLine($"[INFO] 开始导出 {configCollectionsName}");
                    Console.Out.Flush();
                    int exitCode = RunLubanGen(lubanInfo.LubanConfigPath, configType);
                    if (exitCode != 0)
                    {
                        Console.WriteLine($"[ERROR] Luban 导出失败 {configCollectionsName} ExitCode={exitCode}");
                        Console.Out.Flush();
                        return false;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[ERROR] 创建 LubanConf失败 {configCollectionsName} {e.Message}");
                    Console.Out.Flush();
                    return false;
                }
            }

            return true;
        }

        private static int RunLubanGen(string configCollectionPath, string configType)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return RunProcess("pwsh.exe", $"-ExecutionPolicy Bypass -File {Path.Combine(configCollectionPath, "LubanGen.ps1")} -ConfigType {configType}");
            }
            else
            {
                return RunProcess("/usr/local/bin/pwsh", $"-ExecutionPolicy Bypass -File {Path.Combine(configCollectionPath, "LubanGen.ps1")} -ConfigType {configType}");
            }
        }

        public static void ChangePermissions(string filePath, string permissions)
        {
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = "/bin/chmod",
                    Arguments = $"{permissions} \"{filePath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(processInfo))
                {
                    process.WaitForExit();
                    string output = process.StandardOutput.ReadToEnd();

                    // You can handle the output if needed  
                }
            }
            catch (System.Exception e)
            {
                Console.WriteLine($"[ERROR] Failed to change file permissions: {e.Message}");
                Console.Out.Flush();
            }
        }

        private static int RunProcess(string exe, string arguments, string workingDirectory = ".", bool waitExit = true)
        {
            var redirectStandardOutput = false;
            var redirectStandardError = false;
            var useShellExecute = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            if (waitExit)
            {
                redirectStandardOutput = true;
                redirectStandardError = true;
                useShellExecute = false;
            }

            var ImportAllOutput = new StringBuilder();
            var ImportAllError = new StringBuilder();
            var importProcess = new Process();
            importProcess.StartInfo.FileName = exe;
            importProcess.StartInfo.Arguments = arguments;
            importProcess.StartInfo.WorkingDirectory = workingDirectory;
            importProcess.StartInfo.UseShellExecute = useShellExecute;
            importProcess.StartInfo.CreateNoWindow = true;
            importProcess.StartInfo.RedirectStandardOutput = redirectStandardOutput;
            importProcess.StartInfo.RedirectStandardError = redirectStandardError;
            importProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            importProcess.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            importProcess.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    // 实时输出每一行到控制台
                    Console.WriteLine($"[LUBAN] {args.Data}");
                    Console.Out.Flush();

                    if (args.Data.Contains("ERROR"))
                    {
                        ImportAllError.AppendLine(args.Data);
                    }

                    ImportAllOutput.AppendLine(args.Data);
                }
            };
            importProcess.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    // 实时输出错误信息到控制台
                    Console.WriteLine($"[LUBAN ERROR] {args.Data}");
                    Console.Out.Flush();

                    ImportAllError.AppendLine(args.Data);
                }
            };

            try
            {
                importProcess.Start();
                importProcess.BeginOutputReadLine();
                importProcess.BeginErrorReadLine();
                if (!importProcess.WaitForExit(60000))
                {
                    importProcess.Kill(entireProcessTree: true);
                    Console.WriteLine($"[LUBAN ERROR] 导出超时，已终止进程: {exe} {arguments}");
                    Console.Out.Flush();
                    return -1;
                }

                importProcess.WaitForExit();

                // 由于已经实时输出了，这里只输出总结信息
                var error = ImportAllError.ToString();
                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"[LUBAN] 导出过程中有错误，请检查上面的输出");
                    Console.Out.Flush();
                }
                else
                {
                    Console.WriteLine($"[LUBAN] 导出完成");
                    Console.Out.Flush();
                }
                return importProcess.ExitCode;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[LUBAN ERROR] 导出执行报错: {e.Message}");
                Console.Out.Flush();
            }
            finally
            {
                importProcess.Close();
            }
            return -1;
        }
    }
}
