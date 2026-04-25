using System.Text.Json;

namespace Chip8Emulator.Desktop;

public sealed class Settings
{
    public int InstructionsPerSecond { get; set; } = 600;
    public bool ShiftUsesVy { get; set; } = false;
    public bool JumpUsesVx { get; set; } = true;
    public bool LoadStoreIncrementsI { get; set; } = true;
    public bool LogicResetsVf { get; set; } = false;
    public bool SpritesWrap { get; set; } = true;
    public bool DisplayWait { get; set; } = false;
    public bool VfResultWrittenLast { get; set; } = false;
    public List<string> RecentRoms { get; set; } = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public static Settings Load()
    {
        var path = AppPaths.SettingsFile;
        if (!File.Exists(path)) return new Settings();
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Settings>(json, JsonOptions) ?? new Settings();
        }
        catch
        {
            return new Settings();
        }
    }

    public void Save()
    {
        AppPaths.EnsureDirectory();
        var json = JsonSerializer.Serialize(this, JsonOptions);
        var path = AppPaths.SettingsFile;
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, json);
        if (File.Exists(path)) File.Delete(path);
        File.Move(tmp, path);
    }

    public Settings Clone() => new()
    {
        InstructionsPerSecond = InstructionsPerSecond,
        ShiftUsesVy = ShiftUsesVy,
        JumpUsesVx = JumpUsesVx,
        LoadStoreIncrementsI = LoadStoreIncrementsI,
        LogicResetsVf = LogicResetsVf,
        SpritesWrap = SpritesWrap,
        DisplayWait = DisplayWait,
        VfResultWrittenLast = VfResultWrittenLast,
        RecentRoms = new List<string>(RecentRoms),
    };

    public void PushRecentRom(string path)
    {
        RecentRoms.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
        RecentRoms.Insert(0, path);
        const int max = 10;
        if (RecentRoms.Count > max)
        {
            RecentRoms.RemoveRange(max, RecentRoms.Count - max);
        }
    }
}
