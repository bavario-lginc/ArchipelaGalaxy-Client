using System.Collections.Generic;
using ArchipelaGalaxy.Helpers;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace ArchipelaGalaxy.Views
{
    public partial class DolphinToolPrompt : Window
    {
        public DolphinToolPrompt ()
        {
            InitializeComponent();
        }

        public async void Select(object? sender, RoutedEventArgs args)
        {
            IReadOnlyList<FilePickerFileType> PickerExtension = [new("Executable") { Patterns = ["*.exe", "dolphin-tool", "DolphinTool*"] }];
            FilePickerOpenOptions OpenOptions = new() {
                AllowMultiple = false,
                FileTypeFilter = PickerExtension,
                Title = "Please select your DolphinTool executable (in your Dolphin Emulator folder)"
            };
            var files = await AvaloniaHelper.OpenFilePicker(this, OpenOptions);
            if (files is not null && files.Count > 0)
            {
                DolphinHelper.DolphinToolPath = files[0].Path.LocalPath;
                Close();
            }
            else
            {
                var errorWindow = new ErrorWindow("Invalid DolphinTool executable", "You specified an invalid DolphinTool executable.");
                await errorWindow.ShowDialog(this);
            }
        }

        public async void Download(object? sender, RoutedEventArgs args)
        {
            if (await DolphinHelper.DownloadDolphinToolAsync())
            {
                Close();
            }
            else
            {
                var errorWindow = new ErrorWindow("Invalid DolphinTool executable", "Failed to download DolphinTool executable.");
                await errorWindow.ShowDialog(this);
            }
        }

        public async void ScanRunningExe(object? sender, RoutedEventArgs args)
        {
            if (DolphinHelper.TryGetDolphinPathsWhileRunning() && DolphinHelper.DolphinToolPath is not null)
            {
                Close();
            }
            else
            {
                var errorWindow = new ErrorWindow("Cannot find DolphinTool", "Dolphin Emulator is either not running or DolphinTool was not found through it.\nPlease try again or pick another method.");
                await errorWindow.ShowDialog(this);
            }
        }
    }
}