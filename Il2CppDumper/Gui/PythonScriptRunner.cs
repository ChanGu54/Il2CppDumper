using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppDumper
{
    public class DumpScriptItem
    {
        public string FileName { get; set; }
        public string DisplayName { get; set; }
        public bool Standalone { get; set; }
        public string ToolHint { get; set; }
        public string OutputHeaderName { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public static class PythonScriptRunner
    {
        public static readonly DumpScriptItem[] Scripts =
        {
            new DumpScriptItem
            {
                FileName = "il2cpp_header_to_ghidra.py",
                DisplayName = "il2cpp_header_to_ghidra.py",
                Standalone = true,
                OutputHeaderName = "il2cpp_ghidra.h"
            },
            new DumpScriptItem
            {
                FileName = "il2cpp_header_to_binja.py",
                DisplayName = "il2cpp_header_to_binja.py",
                Standalone = true,
                OutputHeaderName = "il2cpp_binja.h"
            },
            new DumpScriptItem { FileName = "ida_py3.py", DisplayName = "ida_py3.py (IDA)", ToolHint = "IDA" },
            new DumpScriptItem { FileName = "ida_with_struct_py3.py", DisplayName = "ida_with_struct_py3.py (IDA)", ToolHint = "IDA" },
            new DumpScriptItem { FileName = "ghidra.py", DisplayName = "ghidra.py (Ghidra)", ToolHint = "Ghidra" },
            new DumpScriptItem { FileName = "ghidra_with_struct.py", DisplayName = "ghidra_with_struct.py (Ghidra)", ToolHint = "Ghidra" },
            new DumpScriptItem { FileName = "ghidra_wasm.py", DisplayName = "ghidra_wasm.py (Ghidra)", ToolHint = "Ghidra" },
            new DumpScriptItem { FileName = "hopper-py3.py", DisplayName = "hopper-py3.py (Hopper)", ToolHint = "Hopper" },
        };

        public static string ScriptDirectory => AppDomain.CurrentDomain.BaseDirectory;

        public static bool TryFindPython(out string fileName, out List<string> prefixArgs)
        {
            var candidates = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new[]
                {
                    (FileName: "py", Prefix: new[] { "-3" }),
                    (FileName: "python3", Prefix: Array.Empty<string>()),
                    (FileName: "python", Prefix: Array.Empty<string>())
                }
                : new[]
                {
                    (FileName: "python3", Prefix: Array.Empty<string>()),
                    (FileName: "python", Prefix: Array.Empty<string>())
                };

            foreach (var candidate in candidates)
            {
                if (TryRun(candidate.FileName, Combine(candidate.Prefix, "--version")))
                {
                    fileName = candidate.FileName;
                    prefixArgs = new List<string>(candidate.Prefix);
                    return true;
                }
            }

            fileName = null;
            prefixArgs = null;
            return false;
        }

        public static Task<(int ExitCode, string Output)> RunAsync(DumpScriptItem script, string outputDir)
        {
            return Task.Run(() => Run(script, outputDir));
        }

        static (int ExitCode, string Output) Run(DumpScriptItem script, string outputDir)
        {
            if (!TryFindPython(out var python, out var prefixArgs))
            {
                return (-1, "ERROR: Python 3 not found. Install Python 3 and make sure it is on PATH.");
            }

            var scriptPath = Path.Combine(ScriptDirectory, script.FileName);
            if (!File.Exists(scriptPath))
            {
                return (-1, $"ERROR: {script.FileName} not found.");
            }

            var headerPath = Path.Combine(outputDir, "il2cpp.h");
            if (!File.Exists(headerPath))
            {
                return (-1, "ERROR: il2cpp.h not found. Dump with Generate struct / scripts enabled first.");
            }

            var outputHeader = Path.Combine(outputDir, script.OutputHeaderName ?? "output.h");
            var psi = new ProcessStartInfo
            {
                FileName = python,
                WorkingDirectory = outputDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            foreach (var arg in prefixArgs)
            {
                psi.ArgumentList.Add(arg);
            }
            psi.ArgumentList.Add(scriptPath);
            psi.ArgumentList.Add(headerPath);
            psi.ArgumentList.Add(outputHeader);

            using (var process = Process.Start(psi))
            {
                if (process == null)
                {
                    return (-1, "ERROR: Failed to start Python.");
                }
                var stdout = process.StandardOutput.ReadToEnd();
                var stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();
                var output = (stdout + stderr).Trim();
                if (process.ExitCode == 0 && string.IsNullOrEmpty(output))
                {
                    output = $"Done: {outputHeader}";
                }
                return (process.ExitCode, output);
            }
        }

        public static string CopyToOutput(DumpScriptItem script, string outputDir)
        {
            var source = Path.Combine(ScriptDirectory, script.FileName);
            if (!File.Exists(source))
            {
                return $"ERROR: {script.FileName} not found.";
            }

            Directory.CreateDirectory(outputDir);
            var dest = Path.Combine(outputDir, script.FileName);
            File.Copy(source, dest, true);
            return $"Copied {script.FileName} to output.\nRun it from {script.ToolHint}: File → Script file (use script.json / il2cpp.h in the same folder).";
        }

        static bool TryRun(string fileName, IEnumerable<string> args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                foreach (var arg in args)
                {
                    psi.ArgumentList.Add(arg);
                }
                using (var process = Process.Start(psi))
                {
                    if (process == null)
                    {
                        return false;
                    }
                    process.WaitForExit(4000);
                    return !process.HasExited || process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        static string[] Combine(string[] prefix, string extra)
        {
            var result = new string[prefix.Length + 1];
            prefix.CopyTo(result, 0);
            result[prefix.Length] = extra;
            return result;
        }
    }
}
