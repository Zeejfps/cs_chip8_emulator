namespace Chip8Emulator.Desktop;

internal static class AppPaths
{
    public static string AppData
    {
        get
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrEmpty(root))
            {
                root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            return Path.Combine(root, "Chip8Emulator");
        }
    }

    public static string FlagsFile => Path.Combine(AppData, "flags.bin");
    public static string SettingsFile => Path.Combine(AppData, "settings.json");

    public static void EnsureDirectory()
    {
        Directory.CreateDirectory(AppData);
    }
}
