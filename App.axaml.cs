using ArchipelaGalaxy.ViewModels;
using ArchipelaGalaxy.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Fonts;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ArchipelaGalaxy;

public sealed class AppFontCollection : EmbeddedFontCollection {
    public AppFontCollection() : base(
        new Uri("fonts:nf", UriKind.Absolute),
        new Uri("avares://SyatiManager/Assets/Fonts", UriKind.Absolute)) {
    }
}

public partial class App : Application {
    public override void Initialize() {
        AvaloniaXamlLoader.Load(this);
    }

    public void ShowError (string error)
    {
        var errorWindow = new ErrorWindow("Exception", error);
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && ApplicationLifetime is not null) {
            errorWindow.ShowDialog(desktop.MainWindow!);
        }
        else
        {
            errorWindow.Show();
        }
    }

    private void OpenDolphinToolPrompt (IClassicDesktopStyleApplicationLifetime desktop)
    {
        var dolphinToolPrompt = new DolphinToolPrompt();
        desktop.MainWindow = dolphinToolPrompt;
        dolphinToolPrompt.Closed += (_, _) =>
        {
            if (DolphinHelper.DolphinToolPath is not null)
            {
                OpenIsoWindow(desktop);
            }
        };
    }

    private void OpenIsoWindow (IClassicDesktopStyleApplicationLifetime desktop)
    {
        Console.WriteLine("Opening IsoWindow");
        var isoWindow = new IsoWindow();
        isoWindow.Show();
        desktop.MainWindow = isoWindow;
        isoWindow.Closed += (_, _) =>
        {
            if (Builder.IsGeneratedMod())
            {
                OpenMainWindow(desktop);
            }
        };
    }

    private void OpenMainWindow (IClassicDesktopStyleApplicationLifetime desktop)
    {
        desktop.MainWindow = new MainWindow();
        desktop.MainWindow.Show();
    }

    public override void OnFrameworkInitializationCompleted() {
        Avalonia.Threading.Dispatcher.UIThread.UnhandledException += (_, e) => {
            e.Handled = true;
            ShowError(e.Exception.ToString());
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Avalonia.Threading.Dispatcher.UIThread.Post(() => ShowError(((Exception)e.ExceptionObject).ToString()));
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            e.SetObserved();
            Avalonia.Threading.Dispatcher.UIThread.Post(() => ShowError(e.Exception.ToString()));
        };
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            Config.Init();
            DolphinHelper.Init();
            if (!Builder.IsGeneratedMod())
            {
                if (DolphinHelper.DolphinToolPath is null)
                {
                    OpenDolphinToolPrompt(desktop);
                }
                else
                {
                    OpenIsoWindow(desktop);
                }
            }
            else
            {
                OpenMainWindow(desktop);
            }
        }
        
        base.OnFrameworkInitializationCompleted();
    }
}