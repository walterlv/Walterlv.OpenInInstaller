using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Walterlv.Utils;

namespace Walterlv.OpenInInstaller
{
    class InstallerContainer
    {
        public string ProductName { get; }
        public string InstallerFilePattern { get; }
        public string ExecutablePath { get; }
        public IReadOnlyCollection<string> ProcessesToKill { get; }

        public InstallerContainer(string productName, string installerFilePattern, string executablePath,
            IReadOnlyCollection<string> processesToKill)
        {
            ProductName = productName ?? throw new ArgumentNullException(nameof(productName));
            InstallerFilePattern = installerFilePattern ?? throw new ArgumentNullException(nameof(installerFilePattern));
            ExecutablePath = executablePath ?? throw new ArgumentNullException(nameof(executablePath));
            ProcessesToKill = processesToKill ?? new[] { Path.GetFileNameWithoutExtension(executablePath) };
        }

        public void Run()
        {
            var installers = new DirectoryInfo(Directory.GetCurrentDirectory())
                .EnumerateFiles(InstallerFilePattern).Reverse().ToList();

            Console.WriteLine();
            if (installers.Count == 0)
            {
                Console.WriteLine($"未发现任何 {ProductName} 安装包，请将此程序放到你下载了安装包的地方。");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine($"选择你需要在容器中启动的 {ProductName} 安装包：");
            }
            Console.WriteLine();
            for (int i = 0; i < installers.Count; i++)
            {
                Console.Write("[");
                Console.Write((i + 1).ToString().PadLeft(2, ' '));
                Console.Write("] ");
                Console.WriteLine(GetVersion(installers[i].Name));
            }
            Console.WriteLine();
            var selection = ReadInt32("Run: ", 1) - 1;

            var version = GetVersion(installers[selection].Name);
            var executablePath = ExecutablePath
                .Replace("{ProductName}", ProductName)
                .Replace("{Version}", version);
            ExtractInstaller(installers[selection].FullName, $"{ProductName}_{version}");
            var executable = Path.Combine(executablePath, executablePath);

            Console.Write("正在运行，按下 Ctrl+C 退出……");
            var process = Process.Start(executable);
            Console.CancelKeyPress += OnCancelKeyPress;
            process.WaitForExit();

            void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
            {
                e.Cancel = true;
                Console.CancelKeyPress -= OnCancelKeyPress;
                KillProcesses("easinote.cloud", "easinote", "swenserver",
                    "seewouploadservice", "easinote.remoteprocess", "easiagent");
            }
        }

        private int ReadInt32(string label, int @default = 0)
        {
            while (true)
            {
                Console.Write(label);
                Console.Write(@default);
                Console.CursorLeft = label.Length;
                var selection = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(selection))
                {
                    return @default;
                }
                else
                {
                    if (int.TryParse(selection, out var value))
                    {
                        return value;
                    }
                }
            }
        }

        private string GetVersion(string name)
        {
            var match = Regex.Match(name, @"[\d\.]{3,}");
            var version = match.Value;
            return version;
        }

        private void ExtractInstaller(string fileName, string targetDirectory)
        {
            var extension = Path.GetExtension(fileName);
            if (".exe".Equals(extension))
            {
                if (!Directory.Exists(targetDirectory))
                {
                    Console.Write("正在安装……");
                    UnzipInstaller(fileName, targetDirectory);
                    Console.CursorLeft = 0;
                    Console.Write("          ");
                    Console.CursorLeft = 0;
                }
                return;
            }
            throw new InvalidOperationException("无法安装指定类型的安装包。");
        }

        private void UnzipInstaller(string fileName, string outputDirectory)
        {
            var sourceDirectory = Path.GetDirectoryName(fileName);
            var sevenZ = new CommandRunner(
                Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "tools", "7z.exe"),
                sourceDirectory);
            _ = sevenZ.Run($@"x ""{fileName}"" -o""{outputDirectory}""");
        }

        private void KillProcesses(params string[] killProcessNames)
        {
            foreach (var processName in killProcessNames)
            {
                KillProcess(processName);
            }

            void KillProcess(string processName)
            {
                foreach (var process in Process.GetProcessesByName(processName))
                {
                    try
                    {
                        Console.Write($" {process.ProcessName}");
                        process.Kill();
                        process.WaitForExit(1000);
                    }
                    catch (Win32Exception ex)
                    {
                        // 无法结束进程，原因不明。此异常需要记录，留待完善此程序。
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(ex);
                        Console.ResetColor();
                    }
                    catch (InvalidOperationException)
                    {
                        // 进程已经退出，无法继续退出。既然已经退了，那这里也算是退出成功了。
                    }
                }
            }
        }
    }
}
