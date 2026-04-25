using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Chip8Emulator.Desktop;

public sealed partial class SettingsWindow : Window
{
    private readonly Settings _draft;

    public SettingsWindow() : this(new Settings()) { }

    public SettingsWindow(Settings draft)
    {
        _draft = draft;
        InitializeComponent();
        IpsBox.Value = _draft.InstructionsPerSecond;
        ShiftUsesVy.IsChecked = _draft.ShiftUsesVy;
        JumpUsesVx.IsChecked = _draft.JumpUsesVx;
        LoadStoreIncrementsI.IsChecked = _draft.LoadStoreIncrementsI;
        LogicResetsVf.IsChecked = _draft.LogicResetsVf;
        SpritesWrap.IsChecked = _draft.SpritesWrap;
        DisplayWait.IsChecked = _draft.DisplayWait;
        VfResultWrittenLast.IsChecked = _draft.VfResultWrittenLast;
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        _draft.InstructionsPerSecond = (int)(IpsBox.Value ?? 600);
        _draft.ShiftUsesVy = ShiftUsesVy.IsChecked ?? false;
        _draft.JumpUsesVx = JumpUsesVx.IsChecked ?? false;
        _draft.LoadStoreIncrementsI = LoadStoreIncrementsI.IsChecked ?? false;
        _draft.LogicResetsVf = LogicResetsVf.IsChecked ?? false;
        _draft.SpritesWrap = SpritesWrap.IsChecked ?? false;
        _draft.DisplayWait = DisplayWait.IsChecked ?? false;
        _draft.VfResultWrittenLast = VfResultWrittenLast.IsChecked ?? false;
        Close(_draft);
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
