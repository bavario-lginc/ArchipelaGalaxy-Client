using System;
using ArchipelaGalaxy.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ArchipelaGalaxy.Helpers;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using Avalonia.Media;
using System.Text;
using System.Threading.Tasks;

namespace ArchipelaGalaxy.Views {
    public partial class IsoWindow : Window {
        public IsoWindowViewModel ViewModel {
            get => (IsoWindowViewModel)DataContext!;
            set => DataContext = value;
        }

        public IsoWindow() {
            InitializeComponent();
            ViewModel = new IsoWindowViewModel();

            if (DolphinHelper.smg1path is not null)
            {
                ValidateSMG1(DolphinHelper.GetSMG1RegionPretty() ?? "");
            }
            if (DolphinHelper.smg2path is not null)
            {
                ValidateSMG2(DolphinHelper.GetSMG2RegionPretty() ?? "");
            }
            InitDolphinPath();
        }

        public async Task InitDolphinPath ()
        {
            string? dolphinTool = DolphinHelper.GetDolphinPaths().Item2;
            if (dolphinTool is null)
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
                }
                else
                {
                    var errorWindow = new ErrorWindow("Invalid Dolphin Emulator folder", "You specified an invalid Dolphin Emulator folder. Please ensure that the following files exist:\n\t- Dolphin(.exe)\n\t- DolphinTool(.exe)");
                    await errorWindow.ShowDialog(this);
                }
            }
        }

        private void ValidateSMG2 (string region)
        {
            Step1LogoGray.IsVisible = false;
            Step1Logo.IsVisible = true;
            Step1Check.IsVisible = true;
            Step1Region.Text = "Region: " + region;
            Step1Region.IsVisible = true;
            Step1.Foreground = new SolidColorBrush(Colors.White);
            GenerateBtn.IsEnabled = true;
            if (DolphinHelper.smg1path == null)
            {
                GenerateBtn.Content = "Generate Mod (without Ice Flower)";
            }
            else
            {
                GenerateBtn.Content = "Generate Mod";
            }
        }
        private void InvalidateSMG2 ()
        {
            Step1LogoGray.IsVisible = true;
            Step1Logo.IsVisible = false;
            Step1Check.IsVisible = false;
            Step1Region.IsVisible = false;
            Step1.Foreground = new SolidColorBrush(Colors.Gray);
            GenerateBtn.IsEnabled = false;
        }
        private void ValidateSMG1 (string region)
        {
            Step2LogoGray.IsVisible = false;
            Step2Logo.IsVisible = true;
            Step2Check.IsVisible = true;
            Step2Region.Text = "Region: " + region;
            Step2Region.IsVisible = true;
            Step2.Foreground = Step2Explanation.Foreground = new SolidColorBrush(Colors.White);
            GenerateBtn.Content = "Generate Mod";
        }
        private void InvalidateSMG1 ()
        {
            Step2LogoGray.IsVisible = true;
            Step2Logo.IsVisible = false;
            Step2Check.IsVisible = false;
            Step2Region.IsVisible = false;
            Step2.Foreground = Step2Explanation.Foreground = new SolidColorBrush(Colors.Gray);
            GenerateBtn.Content = "Generate Mod (without Ice Flower)";
        }

        public async void SelectSMG2Iso(object? sender, RoutedEventArgs args)
        {
            await InitDolphinPath();
            if (DolphinHelper.DolphinPath is null)
                return;

            IReadOnlyList<FilePickerFileType> PickerExtension = [new("Disc Image (.iso/.wbfs/.rvz/.gcz/.wia)") { Patterns = DolphinHelper.SupportedExtensions }];
            FilePickerOpenOptions OpenOptions = new() {
                AllowMultiple = false,
                FileTypeFilter = PickerExtension,
                Title = "Select SMG2 Game"
            };
            var files = await AvaloniaHelper.OpenFilePicker(this, OpenOptions);

            if (files is not null && files.Count > 0) {
                string? smg2Path = DolphinHelper.LoadSMG2(files[0].Path.LocalPath);
                if (smg2Path == null)
                {
                    if (DolphinHelper.smg2IsoPath is null) 
                        InvalidateSMG2();
                    var errorWindow = new ErrorWindow("Error while parsing SMG2 Game file", "The Game file you provided is either not a valid Super Mario Galaxy 2 Game File, or there was an issue extracting it.\n\nMake sure you are providing Super Mario Galaxy 2 with one of these game ids:\n\t- SB4E (USA)\n\t- SB4P (PAL)\n\t- SB4J (JPN)\n\t- SB4K (KOR)\n\t- SB4W (TWN)");
                    await errorWindow.ShowDialog(this);
                    return;
                }
                await PatchDownloader.GetPatch(DolphinHelper.smg2GameIdWithRegion);
                ValidateSMG2(DolphinHelper.GetSMG2RegionPretty() ?? "");
            }
        }

        public async void SelectSMG1Iso(object? sender, RoutedEventArgs args)
        {
            await InitDolphinPath();
            if (DolphinHelper.DolphinPath is null)
                return;

            IReadOnlyList<FilePickerFileType> PickerExtension = [new("Disc Image (.iso/.wbfs/.rvz/.gcz/.wia)") { Patterns = DolphinHelper.SupportedExtensions }];
            FilePickerOpenOptions OpenOptions = new() {
                AllowMultiple = false,
                FileTypeFilter = PickerExtension,
                Title = "Select SMG1 Game"
            };
            var files = await AvaloniaHelper.OpenFilePicker(this, OpenOptions);

            if (files is not null && files.Count > 0) {
                string? smg1Path = DolphinHelper.LoadSMG1(files[0].Path.LocalPath);
                if (smg1Path == null)
                {
                    if (DolphinHelper.smg1IsoPath is null) 
                        InvalidateSMG1();
                    var errorWindow = new ErrorWindow("Invalid SMG1 Game", "The Game you provided is not a valid Super Mario Galaxy Game File.\n\nMake sure you are providing Super Mario Galaxy (1) with one of these game ids:\n\t- RMGE (USA)\n\t- RMGP (PAL)\n\t- RMGJ (JPN)\n\t- RMGK (KOR)");
                    await errorWindow.ShowDialog(this);
                    return;
                }
                ValidateSMG1(DolphinHelper.GetSMG1RegionPretty() ?? "");
            }
        }
        public async void GenerateMod(object? sender, RoutedEventArgs args)
        {
            FolderPickerOpenOptions OpenOptions = new() {
                AllowMultiple = false,
                Title = "Select Patch Destination Folder",
            };
            var folder = await AvaloniaHelper.OpenFolderPicker(this, OpenOptions);
            if (DolphinHelper.smg2path is not null && DolphinHelper.smg2GameIdWithRegion is not null && folder is not null && folder.Count > 0)
            {
                bool success = await Builder.GenerateMod(DolphinHelper.smg2path, DolphinHelper.smg2GameIdWithRegion, folder[0].Path.LocalPath);
                if (success)
                {
                    Close();
                }
            }
        }
    }
}