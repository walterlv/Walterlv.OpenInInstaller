using System;
using System.Reflection;

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
            var productName = "EasiNote5";
            Console.Title = $"{productName} 安装包容器";
            Console.WriteLine($"{productName} 安装包容器，版本 {Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>().Version}。");

            var ic = new InstallerContainer(productName, "EasiNoteSetup_*.exe",
                @"\{ProductName}_{ProductVersion}\Main\EasiNote.exe",
                new[] { "easinote.cloud", "easinote", "swenserver",
                    "seewouploadservice", "easinote.remoteprocess", "easiagent"});
            ic.Run();

            return 0;
        }

        private static int HandleSingleFile(string fileName)
        {
            // ExtractInstaller(fileName, Path.GetFileNameWithoutExtension(fileName));
            return 0;
        }
    }
}
