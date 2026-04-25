using Chip8Emulator.Core;

namespace Chip8Emulator.Desktop;

internal sealed class DesktopFlagStore : IFlagStore
{
    private readonly string _path = AppPaths.FlagsFile;

    public void LoadInto(Span<byte> destination)
    {
        if (!File.Exists(_path)) return;
        try
        {
            var bytes = File.ReadAllBytes(_path);
            bytes.AsSpan(0, Math.Min(bytes.Length, destination.Length)).CopyTo(destination);
        }
        catch (IOException)
        {
        }
    }

    public void SaveFrom(ReadOnlySpan<byte> source)
    {
        AppPaths.EnsureDirectory();
        var tmp = _path + ".tmp";
        File.WriteAllBytes(tmp, source.ToArray());
        if (File.Exists(_path)) File.Delete(_path);
        File.Move(tmp, _path);
    }
}
