namespace Emulator.Api;

public interface IDisplay
{
    void Clear();
    void Update();
    byte BlitRow(byte x, byte y, byte row);
}