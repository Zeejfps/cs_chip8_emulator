using Chip8Emulator.Core;

namespace Chip8Emulator.Cli;

public sealed class FilePersistentFlags : IPersistentFlags
{
    private readonly string _path;

    public FilePersistentFlags(string? path = null)
    {
        _path = path ?? DefaultPath();
    }

    public void Read(Span<byte> destination)
    {
        if (!File.Exists(_path)) return;
        try
        {
            var bytes = File.ReadAllBytes(_path);
            bytes.AsSpan(0, Math.Min(bytes.Length, destination.Length)).CopyTo(destination);
        }
        catch (IOException)
        {
            // Treat transient file errors as "no saved flags".
        }
    }

    public void Write(ReadOnlySpan<byte> source)
    {
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllBytes(_path, source.ToArray());
    }

    private static string DefaultPath()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrEmpty(root))
        {
            root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }
        return Path.Combine(root, "Chip8Emulator", "flags.bin");
    }
}
