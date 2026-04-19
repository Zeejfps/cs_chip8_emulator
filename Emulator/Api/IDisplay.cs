namespace Emulator.Api;

public interface IDisplay
{
    void Clear();
    void Update();
    int BlitRow(byte x, byte y, byte row);
}