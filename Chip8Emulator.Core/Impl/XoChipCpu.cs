using static Chip8Emulator.Core.Chip8Disassembler;

namespace Chip8Emulator.Core.Impl;

// XO-Chip additions: scroll up N, 16-bit I register (long load), bitplane
// selection, audio pattern buffer + pitch, register range load/store.
internal static class XoChipCpu
{
    // ---- 00DN : scroll display up N rows ------------------------------------

    public static void ExecuteScrollUpIns(Chip8Machine machine, int ins)
    {
        machine.ScrollDisplayUp(ins & 0x0F);
    }

    // ---- F000 NNNN : long load I --------------------------------------------

    public static void ExecuteLongLoadIndexRegister(Chip8Machine machine, int ins)
    {
        // F000 NNNN matches only when X is 0; ignore F1nn–FFnn slotted here.
        if (ExtractX(ins) != 0) return;
        var pc = machine.ReadProgramCounter();
        var hi = machine.ReadMemory(pc);
        var lo = machine.ReadMemory(pc + 1);
        machine.WriteIndexRegister((hi << 8) | lo);
        machine.AdvanceProgramCounter();
    }

    // ---- FN01 : select bitplane mask ----------------------------------------

    public static void ExecuteSelectPlaneIns(Chip8Machine machine, int ins)
    {
        machine.SelectedPlanes = (byte)ExtractX(ins);
    }

    // ---- F002 / FX3A : audio pattern buffer + pitch -------------------------

    public static void ExecuteLoadAudioPatternIns(Chip8Machine machine, int ins)
    {
        // F002 — only defined when X == 0; other slots (F102, F202, ...) are undefined.
        if (ExtractX(ins) != 0) return;
        machine.LoadAudioPattern();
    }

    public static void ExecuteSetPitchIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        machine.SetPitch(machine.ReadGeneralPurposeRegister(x));
    }

    // ---- 5XY2 / 5XY3 : store / load register range --------------------------

    public static void ExecuteStoreRegisterRange(Chip8Machine machine, int ins)  // 5XY2
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        var step = x <= y ? 1 : -1;
        var count = Math.Abs(y - x) + 1;
        for (var k = 0; k < count; k++)
        {
            machine.WriteMemory(
                machine.ReadIndexRegisterWithOffset(k),
                machine.ReadGeneralPurposeRegister(x + k * step));
        }
    }

    public static void ExecuteLoadRegisterRange(Chip8Machine machine, int ins)  // 5XY3
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        var step = x <= y ? 1 : -1;
        var count = Math.Abs(y - x) + 1;
        for (var k = 0; k < count; k++)
        {
            var address = machine.ReadIndexRegisterWithOffset(k);
            var value = machine.ReadMemory(address);
            machine.WriteGeneralPurposeRegister(x + k * step, value);
        }
    }
}
