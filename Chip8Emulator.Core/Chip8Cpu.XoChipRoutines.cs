using static Chip8Emulator.Core.Chip8Disassembler;

namespace Chip8Emulator.Core;

// XO-Chip additions: scroll up N, 16-bit I register (long load), bitplane
// selection, audio pattern buffer + pitch, register range load/store.
public sealed partial class Chip8Cpu
{
    // ---- 00DN : scroll display up N rows ------------------------------------

    internal void ScrollUp(int ins)
    {
        _display.ScrollUp(ins & 0x0F);
    }

    // ---- F000 NNNN : long load I --------------------------------------------

    internal void LongLoadIndexRegister(int ins)
    {
        // F000 NNNN matches only when X is 0; ignore F1nn–FFnn slotted here.
        if (ExtractX(ins) != 0) return;
        var pc = ReadProgramCounter();
        var hi = _memory.Read(pc);
        var lo = _memory.Read(pc + 1);
        Registers.WriteI((hi << 8) | lo);
        AdvanceProgramCounter();
    }

    // ---- FN01 : select bitplane mask ----------------------------------------

    internal void SelectPlane(int ins)
    {
        _display.SelectedPlanes = (byte)ExtractX(ins);
    }

    // ---- F002 / FX3A : audio pattern buffer + pitch -------------------------

    internal void LoadAudioPattern(int ins)
    {
        // F002 — only defined when X == 0; other slots (F102, F202, ...) are undefined.
        if (ExtractX(ins) != 0) return;
        _bus.Publish(default(LoadAudioPatternEvent));
    }

    internal void SetPitch(int ins)
    {
        var x = ExtractX(ins);
        var pitch = Registers.ReadV(x);
        _bus.Publish(new SetPitchEvent(pitch));
    }

    // ---- 5XY2 / 5XY3 : store / load register range --------------------------

    internal void StoreRegisterRange(int ins)  // 5XY2
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        var step = x <= y ? 1 : -1;
        var count = Math.Abs(y - x) + 1;
        for (var k = 0; k < count; k++)
        {
            _memory.Write(
                Registers.ReadIWithOffset(k),
                Registers.ReadV(x + k * step));
        }
    }

    internal void LoadRegisterRange(int ins)  // 5XY3
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        var step = x <= y ? 1 : -1;
        var count = Math.Abs(y - x) + 1;
        for (var k = 0; k < count; k++)
        {
            var address = Registers.ReadIWithOffset(k);
            var value = _memory.Read(address);
            Registers.WriteV(x + k * step, value);
        }
    }
}
