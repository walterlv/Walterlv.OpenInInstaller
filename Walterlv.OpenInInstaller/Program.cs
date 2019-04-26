using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Walterlv.Utils;

namespace Walterlv.OpenInInstaller
{
    class Program
    {
        static int Main(string[] args) => args switch
        {
            { Length: 1 } => HandleSingleFile(args[0]),
            _ => HandleWorkingFolder(),
        };

        private static int HandleWorkingFolder()
        {
            var installers = new DirectoryInfo(Directory.GetCurrentDirectory())
                .EnumerateFiles("EasiNoteSetup_*.exe").Reverse().ToList();

            Console.Title = "安装包容器";
            for (int i = 0; i < installers.Count; i++)
            {
                Console.Write("[");
                Console.Write((i + 1).ToString().PadLeft(2, ' '));
                Console.Write("] ");
                Console.WriteLine(GetVersion(installers[i].Name));
            }
            var selection = ReadInt32("Run: ", 1) - 1;
            var directoryName = $"EasiNote5_{GetVersion(installers[selection].Name)}";
            ExtractInstaller(installers[selection].FullName, directoryName);
            var executable = Path.Combine(directoryName, directoryName, "Main", "EasiNote.exe");

            Console.Write("正在运行，按下 Ctrl+C 退出……");
            var process = Process.Start(executable);
            Console.CancelKeyPress += OnCancelKeyPress;
            process.WaitForExit();

            return 0;

            void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
            {
                e.Cancel = true;
                Console.CancelKeyPress -= OnCancelKeyPress;
                KillProcesses("easinote.cloud", "easinote", "swenserver",
                    "seewouploadservice", "easinote.remoteprocess", "easiagent");
            }
        }

        private static int ReadInt32(string label, int @default = 0)
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

        private static string GetVersion(string name)
        {
            var match = Regex.Match(name, @"[\d\.]+");
            var version = match.Value;
            return version;
        }

        private static int HandleSingleFile(string fileName)
        {
            ExtractInstaller(fileName, Path.GetFileNameWithoutExtension(fileName));
            return 0;
        }

        private static void ExtractInstaller(string fileName, string targetDirectory)
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

        private static void UnzipInstaller(string fileName, string outputDirectory)
        {
            var sourceDirectory = Path.GetDirectoryName(fileName);
            var sevenZ = new CommandRunner(
                Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "tools", "7z.exe"),
                sourceDirectory);
            _ = sevenZ.Run($@"x ""{fileName}"" -o""{outputDirectory}""");
        }

        private static void KillProcesses(params string[] killProcessNames)
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
                        // Log.Default.Error(ex);
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
