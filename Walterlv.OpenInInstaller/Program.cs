using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Walterlv.Utils;

namespace Walterlv.OpenInInstaller
{
    class Program
    {
        static int Main(string[] args) => args switch
        {
            { Length: 1 } => HandleSingleFile(args[0]),
            _ => OutputUsage(),
        };

        private static int OutputUsage()
        {
            Console.WriteLine($"Usage: {Process.GetCurrentProcess().ProcessName} XxxSetup_1.0.0S.exe");
            return 0;
        }

        private static int HandleSingleFile(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            if (".exe".Equals(extension))
            {
                var sourceDirectory = Path.GetDirectoryName(fileName);
                var targetDirectory = Path.Combine(sourceDirectory, Path.GetFileNameWithoutExtension(fileName));
                if (!Directory.Exists(targetDirectory))
                {
                    UnzipInstaller(fileName, targetDirectory);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("目标目录已经存在。");
                    Console.ResetColor();
                }
                return 0;
            }

            return 0;
        }

        private static void UnzipInstaller(string fileName, string outputDirectory)
        {
            var sourceDirectory = Path.GetDirectoryName(fileName);
            Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "tools", "7z.exe");
            var sevenZ = new CommandRunner("7z.exe", sourceDirectory);
            _ = sevenZ.Run("");
        }
    }
}
