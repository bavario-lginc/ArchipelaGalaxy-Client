using System.Collections.Generic;
using ArchipelaGalaxy.Helpers;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace ArchipelaGalaxy.Views
{
    public partial class DolphinPrompt : Window
    {
        public DolphinPrompt ()
        {
            InitializeComponent();
        }

        public async void Select(object? sender, RoutedEventArgs args)
        {
            IReadOnlyList<FilePickerFileType> PickerExtension = [new("Executable") { Patterns = ["*.exe", "dolphin-emu", "Dolphin*"] }];
            FilePickerOpenOptions OpenOptions = new() {
                AllowMultiple = false,
                FileTypeFilter = PickerExtension,
                Title = "Please select your Dolphin executable (in your Dolphin Emulator folder)"
            };
            var files = await AvaloniaHelper.OpenFilePicker(this, OpenOptions);
            if (files is not null && files.Count > 0)
            {
                DolphinHelper.DolphinPath = files[0].Path.LocalPath;
                DolphinHelper.LaunchMod();
                Close();
            }
            else
            {
                var errorWindow = new ErrorWindow("Invalid Dolphin executable", "You specified an invalid Dolphin executable.");
                await errorWindow.ShowDialog(this);
            }
        }

        public async void ScanRunningExe(object? sender, RoutedEventArgs args)
        {
            if (DolphinHelper.TryGetDolphinPathsWhileRunning() && DolphinHelper.DolphinPath is not null)
            {
                Close();
            }
            else
            {
                var errorWindow = new ErrorWindow("Cannot find Dolphin", "Dolphin Emulator is either not running or Dolphin was not found through it.\nPlease try again or pick another method.");
                await errorWindow.ShowDialog(this);
            }
        }
    }
}
