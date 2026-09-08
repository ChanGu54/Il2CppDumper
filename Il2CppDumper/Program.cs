using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Il2CppDumper
{
    class Program
    {
        internal static Config Config;

        [STAThread]
        static void Main(string[] args)
        {
            Config = JsonSerializer.Deserialize<Config>(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"config.json"));
            if (args.Length == 0)
            {
                GuiApp.Run();
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                AttachParentConsole();
            }

            RunCli(args);
        }

        static void RunCli(string[] args)
        {
            var config = Config;
            string il2cppPath = null;
            string metadataPath = null;
            string outputDir = null;

            if (args.Length == 1)
            {
                if (args[0] == "-h" || args[0] == "--help" || args[0] == "/?" || args[0] == "/h")
                {
                    ShowHelp();
                    return;
                }
            }
            if (args.Length > 3)
            {
                ShowHelp();
                return;
            }
            if (args.Length > 1)
            {
                foreach (var arg in args)
                {
                    if (File.Exists(arg))
                    {
                        var file = File.ReadAllBytes(arg);
                        if (BitConverter.ToUInt32(file, 0) == 0xFAB11BAF)
                        {
                            metadataPath = arg;
                        }
                        else
                        {
                            il2cppPath = arg;
                        }
                    }
                    else if (Directory.Exists(arg))
                    {
                        outputDir = Path.GetFullPath(arg) + Path.DirectorySeparatorChar;
                    }
                }
            }
            outputDir ??= AppDomain.CurrentDomain.BaseDirectory;
            if (il2cppPath == null)
            {
                ShowHelp();
                return;
            }
            if (metadataPath == null)
            {
                Console.WriteLine($"ERROR: Metadata file not found or encrypted.");
            }
            else
            {
                try
                {
                    var host = new ConsoleDumperHost();
                    if (DumperEngine.Init(il2cppPath, metadataPath, config, host, out var metadata, out var il2Cpp))
                    {
                        DumperEngine.Dump(metadata, il2Cpp, outputDir, config, host);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            if (config.RequireAnyKey)
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine($"usage: {AppDomain.CurrentDomain.FriendlyName} <executable-file> <global-metadata> <output-directory>");
        }

        const int AttachParentProcess = -1;

        static void AttachParentConsole()
        {
            if (!AttachConsole(AttachParentProcess))
            {
                return;
            }
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
            Console.SetIn(new StreamReader(Console.OpenStandardInput()));
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AttachConsole(int dwProcessId);
    }
}
