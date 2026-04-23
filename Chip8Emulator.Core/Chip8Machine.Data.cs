using Chip8Emulator.Core.Routines;

namespace Chip8Emulator.Core;

internal delegate void Routine(ICpu cpu, int ins);

internal sealed partial class Chip8Machine
{
    private static ReadOnlySpan<byte> LowResFont =>
    [
        0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
        0x20, 0x60, 0x20, 0x20, 0x70, // 1
        0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
        0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
        0x90, 0x90, 0xF0, 0x10, 0x10, // 4
        0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
        0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
        0xF0, 0x10, 0x20, 0x40, 0x40, // 7
        0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
        0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
        0xF0, 0x90, 0xF0, 0x90, 0x90, // A
        0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
        0xF0, 0x80, 0x80, 0x80, 0xF0, // C
        0xE0, 0x90, 0x90, 0x90, 0xE0, // D
        0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
        0xF0, 0x80, 0xF0, 0x80, 0x80, // F
    ];

    private static ReadOnlySpan<byte> HighResFont => [
        // 0
        0x3C, 0x7E, 0xE7, 0xC3, 0xC3, 0xC3, 0xC3, 0xE7, 0x7E, 0x3C,
        // 1
        0x18, 0x38, 0x58, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x3C,
        // 2
        0x3E, 0x7F, 0xC3, 0x06, 0x0C, 0x18, 0x30, 0x60, 0xFF, 0xFF,
        // 3
        0x3C, 0x7E, 0xC3, 0x03, 0x0E, 0x0E, 0x03, 0xC3, 0x7E, 0x3C,
        // 4
        0x06, 0x0E, 0x1E, 0x36, 0x66, 0xC6, 0xFF, 0xFF, 0x06, 0x06,
        // 5
        0xFF, 0xFF, 0xC0, 0xC0, 0xFC, 0xFE, 0x03, 0xC3, 0x7E, 0x3C,
        // 6
        0x3E, 0x7C, 0xC0, 0xC0, 0xFC, 0xFE, 0xC3, 0xC3, 0x7E, 0x3C,
        // 7
        0xFF, 0xFF, 0x03, 0x06, 0x0C, 0x18, 0x30, 0x60, 0x60, 0x60,
        // 8
        0x3C, 0x7E, 0xC3, 0xC3, 0x7E, 0x7E, 0xC3, 0xC3, 0x7E, 0x3C,
        // 9
        0x3C, 0x7E, 0xC3, 0xC3, 0x7F, 0x3F, 0x03, 0x03, 0x3E, 0x7C
    ];
    
    private static void NoOp(ICpu cpu, int ins) { }

    private Routine[] LoadMainRoutines()
    {
        var table = new Routine[16];
        table[0x0] = (cpu, ins) => { if ((ins & 0xFF00) == 0x0000) SystemRoutines[ins & 0x00FF](cpu, ins); };
        table[0x1] = Chip8Routines.JumpToAddress;
        table[0x2] = Chip8Routines.CallSubroutine;
        table[0x3] = Chip8Routines.SkipNextInsIfRegisterValueEqualsValue;
        table[0x4] = Chip8Routines.SkipNextInsIfRegisterValueNotEqualsValue;
        table[0x5] = (cpu, ins) => FiveOpRoutines[ins & 0x000F](cpu, ins);
        table[0x6] = Chip8Routines.SetRegisterValue;
        table[0x7] = Chip8Routines.AddValueToRegister;
        table[0x8] = (cpu, ins) => ArithmeticRoutines[ins & 0x000F](cpu, ins);
        table[0x9] = Chip8Routines.SkipNextInsIfRegisterValueNotEqualsRegisterValue;
        table[0xA] = Chip8Routines.SetIndexRegisterIns;
        table[0xB] = Chip8Routines.JumpWithOffsetIns;
        table[0xC] = Chip8Routines.GenerateRandomNum;
        table[0xD] = Chip8Routines.DrawToScreen;
        table[0xE] = (cpu, ins) => KeyCheckRoutines[ins & 0x00FF](cpu, ins);
        table[0xF] = (cpu, ins) => TimerRoutines[ins & 0x00FF](cpu, ins);
        return table;
    }

    private Routine[] LoadSystemRoutines()
    {
        var table = new Routine[256];
        Array.Fill(table, NoOp);
        table[0xE0] = Chip8Routines.ClearDisplay;
        table[0xEE] = Chip8Routines.ReturnFromSubroutine;
        table[0xFF] = SChipRoutines.EnableHiresMode;
        table[0xFE] = SChipRoutines.DisableHiresMode;
        table[0xFB] = SChipRoutines.ScrollRight;
        table[0xFC] = SChipRoutines.ScrollLeft;
        for (var n = 0; n < 16; n++)
        {
            // 00CN — S-CHIP: scroll display down N rows.
            table[0xC0 + n] = SChipRoutines.ScrollDown;
            // 00DN — XO-CHIP: scroll display up N rows.
            table[0xD0 + n] = XoChipRoutines.ScrollUp;
        }
        return table;
    }

    private Routine[] LoadTimerRoutines()
    {
        var table = new Routine[256];
        Array.Fill(table, NoOp);
        // F000 NNNN — XO-CHIP long load I with the 16-bit word following the opcode.
        table[0x00] = XoChipRoutines.LongLoadIndexRegister;
        // FN01 — XO-CHIP select bitplane mask (N = 0..3).
        table[0x01] = XoChipRoutines.SelectPlane;
        // F002 — XO-CHIP copy 16 bytes at [I] into audio pattern buffer.
        table[0x02] = XoChipRoutines.LoadAudioPattern;
        table[0x07] = Chip8Routines.ReadDelayTimer;
        table[0x0A] = Chip8Routines.WaitForKeyPressAndRelease;
        table[0x15] = Chip8Routines.SetDelayTimer;
        table[0x18] = Chip8Routines.SetSoundTimer;
        table[0x1E] = Chip8Routines.AddVxToI;
        table[0x29] = Chip8Routines.LoadLowResFontCharacter;
        table[0x30] = SChipRoutines.LoadHighResFontCharacter;
        table[0x33] = Chip8Routines.StoreBcdInMemory;
        // FX3A — XO-CHIP set audio playback pitch from Vx.
        table[0x3A] = XoChipRoutines.SetPitch;
        table[0x55] = Chip8Routines.StoreRegisters;
        table[0x65] = Chip8Routines.LoadRegisters;
        // FX75 / FX85 — SCHIP save/load V0..Vx to persistent user flags.
        table[0x75] = SChipRoutines.SaveFlags;
        table[0x85] = SChipRoutines.LoadFlags;
        return table;
    }

    private Routine[] LoadKeyCheckRoutines()
    {
        var table = new Routine[256];
        Array.Fill(table, NoOp);
        table[0x9E] = Chip8Routines.SkipNextInsIfKeyIsPressed;
        table[0xA1] = Chip8Routines.SkipNextInsIfKeyIsReleased;
        return table;
    }

    private Routine[] LoadFiveOpRoutines()
    {
        var table = new Routine[16];
        Array.Fill(table, NoOp);
        table[0] = Chip8Routines.SkipIfVxEqualsVy;
        table[2] = XoChipRoutines.StoreRegisterRange;
        table[3] = XoChipRoutines.LoadRegisterRange;
        return table;
    }

    private Routine[] LoadArithmeticRoutines()
    {
        var table = new Routine[16];
        Array.Fill(table, NoOp);
        table[0x0] = Chip8Routines.SetRegisterValueFromRegister;
        table[0x1] = Chip8Routines.BitwiseOrOnRegisters;
        table[0x2] = Chip8Routines.BitwiseAndOnRegisters;
        table[0x3] = Chip8Routines.XorRegisterValueFromRegister;
        table[0x4] = Chip8Routines.AddValueToRegisterWithCarry;
        table[0x5] = Chip8Routines.VxSubVy;
        table[0x6] = Chip8Routines.ShiftRight;
        table[0x7] = Chip8Routines.VySubVx;
        table[0xE] = Chip8Routines.ShiftLeft;
        return table;
    }
}
