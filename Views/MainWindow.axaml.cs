using System;
using ArchipelaGalaxy.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;
using ArchipelaGalaxy.Helpers;
using Avalonia.Platform.Storage;
using System.Collections.Generic;

namespace ArchipelaGalaxy.Views {
    public partial class MainWindow : Window {
        public MainWindowViewModel ViewModel {
            get => (MainWindowViewModel)DataContext!;
            set => DataContext = value;
        }

        public MainWindow() {
            InitializeComponent();
            ViewModel = new MainWindowViewModel();
        }

        private void OpenDolphinPrompt ()
        {
            Console.WriteLine("Opening DolphinPrompt");
            var dolphinPrompt = new DolphinPrompt();
            dolphinPrompt.Show();
        }

        public void OpenDolphin(object? sender, RoutedEventArgs args)
        {
            if (DolphinHelper.DolphinPath is null)
            {
                OpenDolphinPrompt();
            }
            else
            {
                DolphinHelper.LaunchMod();
            }
        }

        public void OpenIsoWindow(object? sender, RoutedEventArgs args)
        {
            Console.WriteLine("Opening IsoWindow");
            var isoWindow = new IsoWindow();
            isoWindow.Show();
            //desktop.MainWindow = isoWindow;
            /*isoWindow.Closed += (_, _) =>
            {
                if (Builder.IsGeneratedMod())
                {
                    desktop.MainWindow = new MainWindow();
                    desktop.MainWindow.Show();
                }
            };*/
        }
    }
}