using System.Runtime.InteropServices.JavaScript;
using Chip8Emulator.Core;

namespace Chip8Emulator.Web;

internal sealed partial class LocalStoragePersistentFlags : IPersistentFlags
{
    public void Read(Span<byte> destination)
    {
        var encoded = ReadStorageJs();
        if (string.IsNullOrEmpty(encoded)) return;
        try
        {
            var bytes = Convert.FromBase64String(encoded);
            bytes.AsSpan(0, Math.Min(bytes.Length, destination.Length)).CopyTo(destination);
        }
        catch (FormatException)
        {
            // corrupted entry — ignore
        }
    }

    public void Write(ReadOnlySpan<byte> source)
    {
        WriteStorageJs(Convert.ToBase64String(source));
    }

    [JSImport("persistentFlags.read", "main.js")]
    private static partial string ReadStorageJs();

    [JSImport("persistentFlags.write", "main.js")]
    private static partial void WriteStorageJs(string base64);
}
