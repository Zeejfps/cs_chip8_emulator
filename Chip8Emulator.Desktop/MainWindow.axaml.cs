using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace Chip8Emulator.Desktop;

public sealed partial class MainWindow : Window
{
    private Settings _settings = Settings.Load();

    public MainWindow()
    {
        InitializeComponent();
        Viewport.Settings = _settings;
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);
        Opened += (_, _) => RebuildRecentRomsMenu();
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        DragDrop.SetAllowDrop(this, true);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            TogglePause();
            e.Handled = true;
            return;
        }
        if (e.Key == Key.F5)
        {
            Viewport.Reset();
            e.Handled = true;
            return;
        }
        if (e.Key == Key.F11)
        {
            ToggleFullscreen();
            e.Handled = true;
            return;
        }
        if (e.Key == Key.O && (e.KeyModifiers & KeyModifiers.Control) != 0)
        {
            _ = OpenRomDialogAsync();
            e.Handled = true;
            return;
        }

        if (Viewport.Input.TryHandleKeyDown(e.Key))
        {
            e.Handled = true;
        }
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (Viewport.Input.TryHandleKeyUp(e.Key))
        {
            e.Handled = true;
        }
    }

    private async void OnOpenRom(object? sender, RoutedEventArgs e)
    {
        await OpenRomDialogAsync();
    }

    private async Task OpenRomDialogAsync()
    {
        var top = TopLevel.GetTopLevel(this);
        if (top == null) return;
        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open ROM",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("CHIP-8 ROMs")
                {
                    Patterns = ["*.ch8", "*.c8", "*.xo8", "*.sc8", "*.rom"],
                },
                new FilePickerFileType("All files") { Patterns = ["*.*"] },
            ],
        });

        if (files.Count == 0) return;
        var path = files[0].TryGetLocalPath();
        if (path == null) return;
        LoadRomFromPath(path);
    }

    private void LoadRomFromPath(string path)
    {
        try
        {
            var bytes = File.ReadAllBytes(path);
            Viewport.LoadRom(bytes);
            _settings.PushRecentRom(path);
            _settings.Save();
            RebuildRecentRomsMenu();
            Viewport.Focus();
            UpdatePauseMenuLabel();
        }
        catch (Exception ex)
        {
            ShowError($"Could not load ROM:\n{ex.Message}");
        }
    }

    private void RebuildRecentRomsMenu()
    {
        RecentRomsMenu.ItemsSource = null;
        var items = new List<MenuItem>();
        foreach (var path in _settings.RecentRoms)
        {
            var item = new MenuItem { Header = Path.GetFileName(path) };
            var captured = path;
            item.Click += (_, _) => LoadRomFromPath(captured);
            items.Add(item);
        }
        if (items.Count == 0)
        {
            items.Add(new MenuItem { Header = "(none)", IsEnabled = false });
        }
        RecentRomsMenu.ItemsSource = items;
    }

    private void OnExit(object? sender, RoutedEventArgs e) => Close();

    private void OnPause(object? sender, RoutedEventArgs e) => TogglePause();

    private void TogglePause()
    {
        if (!Viewport.HasRom) return;
        Viewport.TogglePause();
        UpdatePauseMenuLabel();
    }

    private void UpdatePauseMenuLabel()
    {
        PauseMenuItem.Header = Viewport.IsPaused ? "_Resume" : "_Pause";
    }

    private void OnReset(object? sender, RoutedEventArgs e) => Viewport.Reset();

    private async void OnQuirks(object? sender, RoutedEventArgs e)
    {
        var dlg = new SettingsWindow(_settings.Clone());
        var result = await dlg.ShowDialog<Settings?>(this);
        if (result != null)
        {
            _settings = result;
            _settings.Save();
            Viewport.ApplySettings(_settings);
        }
    }

    private void OnFullscreen(object? sender, RoutedEventArgs e) => ToggleFullscreen();

    private void ToggleFullscreen()
    {
        WindowState = WindowState == WindowState.FullScreen
            ? WindowState.Normal
            : WindowState.FullScreen;
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.Files)) return;
        var files = e.Data.GetFiles();
        if (files == null) return;
        foreach (var f in files)
        {
            var p = f.TryGetLocalPath();
            if (p != null)
            {
                LoadRomFromPath(p);
                return;
            }
        }
    }

    private async void ShowError(string message)
    {
        var dlg = new Window
        {
            Title = "Error",
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new TextBlock
            {
                Text = message,
                Margin = new Thickness(16),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            },
        };
        await dlg.ShowDialog(this);
    }
}
