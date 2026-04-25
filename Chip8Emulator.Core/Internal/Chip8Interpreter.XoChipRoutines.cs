using static Chip8Emulator.Core.Chip8Disassembler;

namespace Chip8Emulator.Core.Internal;

// XO-Chip additions: scroll up N, 16-bit I register (long load), bitplane
// selection, audio pattern buffer + pitch, register range load/store.
internal sealed partial class Chip8Interpreter
{
    // ---- 00DN : scroll display up N rows ------------------------------------

    internal void ScrollUp(int ins)
    {
        Display.ScrollUp(ins & 0x0F);
    }

    // ---- F000 NNNN : long load I --------------------------------------------

    internal void LongLoadIndexRegister(int ins)
    {
        // F000 NNNN matches only when X is 0; ignore F1nn–FFnn slotted here.
        if (ExtractX(ins) != 0) return;
        var pc = Registers.ReadPc();
        var hi = Memory.Read(pc);
        var lo = Memory.Read(pc + 1);
        Registers.WriteI((hi << 8) | lo);
        AdvanceProgramCounter();
    }

    // ---- FN01 : select bitplane mask ----------------------------------------

    internal void SelectPlane(int ins)
    {
        Display.SelectedPlanes = (byte)ExtractX(ins);
    }

    // ---- F002 / FX3A : audio pattern buffer + pitch -------------------------

    internal void LoadAudioPattern(int ins)
    {
        // F002 — only defined when X == 0; other slots (F102, F202, ...) are undefined.
        if (ExtractX(ins) != 0) return;
        _audio.WritePattern(span =>
        {
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = Memory.Read(Registers.ReadIWithOffset(i));
            }
        });
    }

    internal void SetPitch(int ins)
    {
        var x = ExtractX(ins);
        _audio.Pitch = Registers.ReadV(x);
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
            Memory.Write(
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
            var value = Memory.Read(address);
            Registers.WriteV(x + k * step, value);
        }
    }
}
