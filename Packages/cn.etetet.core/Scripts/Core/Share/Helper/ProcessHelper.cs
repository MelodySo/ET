using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Path = System.IO.Path;

namespace ET
{
    public static class ProcessHelper
    {
        public static Process PowerShell(string arguments, string workingDirectory = ".", bool waitExit = false)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Run("pwsh.exe", arguments, workingDirectory, waitExit);
            }

            return Run("/usr/local/bin/pwsh", arguments, workingDirectory, waitExit);
        }
        
        public static Process DotNet(string arguments, string workingDirectory = ".", bool waitExit = false)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Run("dotnet.exe", arguments, workingDirectory, waitExit);
            }

            return Run("/usr/local/share/dotnet/dotnet", arguments, workingDirectory, waitExit);
        }

        public static Process Run(string exe, string arguments, string workingDirectory = ".", bool waitExit = false)
        {
            //Log.Debug($"Process Run exe:{exe} ,arguments:{arguments} ,workingDirectory:{workingDirectory}");
            try
            {
                bool redirectStandardOutput = false;
                bool redirectStandardError = false;
                bool useShellExecute = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                
                if (waitExit)
                {
                    redirectStandardOutput = true;
                    redirectStandardError = true;
                    useShellExecute = false;
                }
                
                ProcessStartInfo info = new()
                {
                    FileName = exe,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = useShellExecute,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = redirectStandardOutput,
                    RedirectStandardError = redirectStandardError,
                    StandardErrorEncoding = redirectStandardError ? Encoding.UTF8 : null,
                    StandardOutputEncoding = redirectStandardOutput ? Encoding.UTF8 : null
                };

                Process process = Process.Start(info);

                if (waitExit)
                {
                    // 异步读取标准输出
                    process.OutputDataReceived += (sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            Log.Debug(args.Data);
                        }
                    };
            
                    // 异步读取错误输出
                    process.ErrorDataReceived += (sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            Log.Error(args.Data);
                        }
                    };
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }

                return process;
            }
            catch (Exception e)
            {
                throw new Exception($"dir: {Path.GetFullPath(workingDirectory)}, command: {exe} {arguments}", e);
            }
        }
    }
}
