using System.Runtime.InteropServices.JavaScript;
using Chip8Emulator.Core;

namespace Chip8Emulator.Web;

internal sealed partial class LocalStorageFlagStore : IFlagStore
{
    public void LoadInto(Span<byte> destination)
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

    public void SaveFrom(ReadOnlySpan<byte> source)
    {
        WriteStorageJs(Convert.ToBase64String(source));
    }

    [JSImport("flagStore.read", "main.js")]
    private static partial string ReadStorageJs();

    [JSImport("flagStore.write", "main.js")]
    private static partial void WriteStorageJs(string base64);
}
