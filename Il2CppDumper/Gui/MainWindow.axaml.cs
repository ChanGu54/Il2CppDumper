using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace Il2CppDumper
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DummyDllCheck.IsChecked = Program.Config.GenerateDummyDll;
            StructCheck.IsChecked = Program.Config.GenerateStruct;
            OutputDirBox.Text = AppDomain.CurrentDomain.BaseDirectory;
            AddHandler(DragDrop.DragOverEvent, OnDragOver);
            AddHandler(DragDrop.DropEvent, OnDrop);
            DragDrop.SetAllowDrop(this, true);
        }

        public void AppendLog(string text, bool newLine)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() => AppendLog(text, newLine));
                return;
            }

            if (newLine)
            {
                if (string.IsNullOrEmpty(LogBox.Text))
                {
                    LogBox.Text = text;
                }
                else
                {
                    LogBox.Text += Environment.NewLine + text;
                }
            }
            else
            {
                LogBox.Text += text;
            }
            LogBox.CaretIndex = LogBox.Text?.Length ?? 0;
        }

        public async Task<string> PromptAsync(string message, bool singleKey)
        {
            var dialog = new PromptWindow(message, singleKey);
            return await dialog.ShowPrompt(this);
        }

        private async void OnBrowseIl2Cpp(object sender, RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Il2Cpp binary file",
                AllowMultiple = false
            });
            if (files.Count > 0)
            {
                Il2CppPathBox.Text = files[0].TryGetLocalPath() ?? files[0].Path.LocalPath;
            }
        }

        private async void OnBrowseMetadata(object sender, RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "global-metadata",
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new FilePickerFileType("global-metadata")
                    {
                        Patterns = new[] { "global-metadata.dat", "*.dat" }
                    },
                    new FilePickerFileType("All files")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });
            if (files.Count > 0)
            {
                MetadataPathBox.Text = files[0].TryGetLocalPath() ?? files[0].Path.LocalPath;
            }
        }

        private async void OnBrowseOutput(object sender, RoutedEventArgs e)
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Output directory",
                AllowMultiple = false
            });
            if (folders.Count > 0)
            {
                OutputDirBox.Text = folders[0].TryGetLocalPath() ?? folders[0].Path.LocalPath;
            }
        }

        private async void OnDumpClick(object sender, RoutedEventArgs e)
        {
            var il2cppPath = Il2CppPathBox.Text?.Trim();
            var metadataPath = MetadataPathBox.Text?.Trim();
            var outputDir = OutputDirBox.Text?.Trim();

            if (string.IsNullOrEmpty(il2cppPath) || !File.Exists(il2cppPath))
            {
                AppendLog("ERROR: Il2Cpp binary file not found.", true);
                return;
            }
            if (string.IsNullOrEmpty(metadataPath) || !File.Exists(metadataPath))
            {
                AppendLog("ERROR: Metadata file not found or encrypted.", true);
                return;
            }
            if (string.IsNullOrEmpty(outputDir))
            {
                outputDir = AppDomain.CurrentDomain.BaseDirectory;
            }
            Directory.CreateDirectory(outputDir);
            outputDir = Path.GetFullPath(outputDir) + Path.DirectorySeparatorChar;

            Program.Config.GenerateDummyDll = DummyDllCheck.IsChecked == true;
            Program.Config.GenerateStruct = StructCheck.IsChecked == true;

            DumpButton.IsEnabled = false;
            LogBox.Text = "";
            var host = new GuiDumperHost(this);
            try
            {
                await Task.Run(() =>
                {
                    if (DumperEngine.Init(il2cppPath, metadataPath, Program.Config, host, out var metadata, out var il2Cpp))
                    {
                        DumperEngine.Dump(metadata, il2Cpp, outputDir, Program.Config, host);
                    }
                });
            }
            catch (OperationCanceledException)
            {
                AppendLog("Cancelled.", true);
            }
            catch (Exception ex)
            {
                AppendLog(ex.ToString(), true);
            }
            finally
            {
                DumpButton.IsEnabled = true;
            }
        }

        private void OnOpenOutputClick(object sender, RoutedEventArgs e)
        {
            var outputDir = OutputDirBox.Text?.Trim();
            if (string.IsNullOrEmpty(outputDir) || !Directory.Exists(outputDir))
            {
                AppendLog("ERROR: Output folder not found.", true);
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo("explorer", outputDir) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", outputDir);
            }
            else
            {
                Process.Start("xdg-open", outputDir);
            }
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.DragEffects = e.DataTransfer?.Contains(DataFormat.File) == true ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            var items = e.DataTransfer?.TryGetFiles();
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                var path = item.TryGetLocalPath();
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }
                if (Directory.Exists(path))
                {
                    OutputDirBox.Text = path;
                    continue;
                }
                if (!File.Exists(path))
                {
                    continue;
                }
                if (IsMetadataFile(path))
                {
                    MetadataPathBox.Text = path;
                }
                else
                {
                    Il2CppPathBox.Text = path;
                }
            }
        }

        private static bool IsMetadataFile(string path)
        {
            var name = Path.GetFileName(path);
            if (name.Equals("global-metadata.dat", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            try
            {
                using var stream = File.OpenRead(path);
                var buffer = new byte[4];
                if (stream.Read(buffer, 0, 4) != 4)
                {
                    return false;
                }
                return BitConverter.ToUInt32(buffer, 0) == 0xFAB11BAF;
            }
            catch
            {
                return false;
            }
        }
    }
}
