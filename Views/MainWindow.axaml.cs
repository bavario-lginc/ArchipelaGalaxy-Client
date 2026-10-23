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

        public void OpenDolphin(object? sender, RoutedEventArgs args)
        {
            DolphinHelper.LaunchMod();
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