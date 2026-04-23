using static Chip8Emulator.Core.Chip8Disassembler;

namespace Chip8Emulator.Core.Cpu;

// XO-Chip additions: scroll up N, 16-bit I register (long load), bitplane
// selection, audio pattern buffer + pitch, register range load/store.
internal static class XoChipInstructionSet
{
    // ---- 00DN : scroll display up N rows ------------------------------------

    public static void ScrollUp(ICpu cpu, int ins)
    {
        cpu.ScrollDisplayUp(ins & 0x0F);
    }

    // ---- F000 NNNN : long load I --------------------------------------------

    public static void LongLoadIndexRegister(ICpu cpu, int ins)
    {
        // F000 NNNN matches only when X is 0; ignore F1nn–FFnn slotted here.
        if (ExtractX(ins) != 0) return;
        var pc = cpu.ReadProgramCounter();
        var hi = cpu.ReadMemory(pc);
        var lo = cpu.ReadMemory(pc + 1);
        cpu.WriteIndexRegister((hi << 8) | lo);
        cpu.AdvanceProgramCounter();
    }

    // ---- FN01 : select bitplane mask ----------------------------------------

    public static void SelectPlane(ICpu cpu, int ins)
    {
        cpu.SelectedPlanes = (byte)ExtractX(ins);
    }

    // ---- F002 / FX3A : audio pattern buffer + pitch -------------------------

    public static void LoadAudioPattern(ICpu cpu, int ins)
    {
        // F002 — only defined when X == 0; other slots (F102, F202, ...) are undefined.
        if (ExtractX(ins) != 0) return;
        cpu.LoadAudioPattern();
    }

    public static void SetPitch(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        cpu.SetPitch(cpu.ReadGeneralPurposeRegister(x));
    }

    // ---- 5XY2 / 5XY3 : store / load register range --------------------------

    public static void StoreRegisterRange(ICpu cpu, int ins)  // 5XY2
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        var step = x <= y ? 1 : -1;
        var count = Math.Abs(y - x) + 1;
        for (var k = 0; k < count; k++)
        {
            cpu.WriteMemory(
                cpu.ReadIndexRegisterWithOffset(k),
                cpu.ReadGeneralPurposeRegister(x + k * step));
        }
    }

    public static void LoadRegisterRange(ICpu cpu, int ins)  // 5XY3
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        var step = x <= y ? 1 : -1;
        var count = Math.Abs(y - x) + 1;
        for (var k = 0; k < count; k++)
        {
            var address = cpu.ReadIndexRegisterWithOffset(k);
            var value = cpu.ReadMemory(address);
            cpu.WriteGeneralPurposeRegister(x + k * step, value);
        }
    }
}
