namespace Chip8Emulator.Core.Cpu;

internal delegate void InstructionHandler(ICpu cpu, int ins);

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

    private InstructionHandler[] BuildRootOpcodeTable()
    {
        var table = new InstructionHandler[16];
        table[0x0] = static (cpu, ins) => { if ((ins & 0xFF00) == 0x0000) cpu.DispatchSystemInstruction(ins); };
        table[0x1] = Chip8InstructionSet.JumpToAddress;
        table[0x2] = Chip8InstructionSet.CallSubroutine;
        table[0x3] = Chip8InstructionSet.SkipNextInsIfRegisterValueEqualsValue;
        table[0x4] = Chip8InstructionSet.SkipNextInsIfRegisterValueNotEqualsValue;
        table[0x5] = Chip8InstructionSet.SkipNextInsIfRegisterValueEqualsRegisterValue;
        table[0x6] = Chip8InstructionSet.SetRegisterValue;
        table[0x7] = Chip8InstructionSet.AddValueToRegister;
        table[0x8] = static (cpu, ins) => cpu.DispatchArithmeticInstruction(ins);
        table[0x9] = Chip8InstructionSet.SkipNextInsIfRegisterValueNotEqualsRegisterValue;
        table[0xA] = Chip8InstructionSet.SetIndexRegisterIns;
        table[0xB] = Chip8InstructionSet.JumpWithOffsetIns;
        table[0xC] = Chip8InstructionSet.GenerateRandomNum;
        table[0xD] = Chip8InstructionSet.DrawToScreen;
        table[0xE] = Chip8InstructionSet.SkipNextInsIfKeyIsPressedOrReleased;
        table[0xF] = (cpu, ins) => TimerRoutines[ins & 0x00FF].Invoke(cpu, ins);
        return table;
    }

    private InstructionHandler[] BuildSystemInsTable()
    {
        var table = new InstructionHandler[256];
        Array.Fill(table, NoOp);
        table[0xE0] = Chip8InstructionSet.ClearDisplay;
        table[0xEE] = Chip8InstructionSet.ReturnFromSubroutine;
        table[0xFF] = SChipInstructionSet.EnableHiresMode;
        table[0xFE] = SChipInstructionSet.DisableHiresMode;
        table[0xFB] = SChipInstructionSet.ScrollRight;
        table[0xFC] = SChipInstructionSet.ScrollLeft;
        for (var n = 0; n < 16; n++)
        {
            // 00CN — S-CHIP: scroll display down N rows.
            table[0xC0 + n] = SChipInstructionSet.ScrollDown;
            // 00DN — XO-CHIP: scroll display up N rows.
            table[0xD0 + n] = XoChipInstructionSet.ScrollUp;
        }
        return table;
    }

    private InstructionHandler[] BuildTimerTable()
    {
        var table = new InstructionHandler[256];
        Array.Fill(table, NoOp);
        // F000 NNNN — XO-CHIP long load I with the 16-bit word following the opcode.
        table[0x00] = XoChipInstructionSet.LongLoadIndexRegister;
        // FN01 — XO-CHIP select bitplane mask (N = 0..3).
        table[0x01] = XoChipInstructionSet.SelectPlane;
        // F002 — XO-CHIP copy 16 bytes at [I] into audio pattern buffer.
        table[0x02] = XoChipInstructionSet.LoadAudioPattern;
        table[0x07] = Chip8InstructionSet.ReadDelayTimer;
        table[0x0A] = Chip8InstructionSet.WaitForKeyPressAndRelease;
        table[0x15] = Chip8InstructionSet.SetDelayTimer;
        table[0x18] = Chip8InstructionSet.SetSoundTimer;
        table[0x1E] = Chip8InstructionSet.AddVxToI;
        table[0x29] = Chip8InstructionSet.LoadLowResFontCharacter;
        table[0x30] = SChipInstructionSet.LoadHighResFontCharacter;
        table[0x33] = Chip8InstructionSet.StoreBcdInMemory;
        // FX3A — XO-CHIP set audio playback pitch from Vx.
        table[0x3A] = XoChipInstructionSet.SetPitch;
        table[0x55] = Chip8InstructionSet.StoreRegisters;
        table[0x65] = Chip8InstructionSet.LoadRegisters;
        // FX75 / FX85 — SCHIP save/load V0..Vx to persistent user flags.
        table[0x75] = SChipInstructionSet.SaveFlags;
        table[0x85] = SChipInstructionSet.LoadFlags;
        return table;
    }

    private InstructionHandler[] BuildKeyCheckTable()
    {
        var table = new InstructionHandler[256];
        Array.Fill(table, NoOp);
        table[0x9E] = Chip8InstructionSet.SkipNextInsIfKeyIsPressed;
        table[0xA1] = Chip8InstructionSet.SkipNextInsIfKeyIsReleased;
        return table;
    }

    private InstructionHandler[] BuildFiveOpTable()
    {
        var table = new InstructionHandler[16];
        Array.Fill(table, NoOp);
        table[0] = Chip8InstructionSet.SkipIfVxEqualsVy;
        table[2] = XoChipInstructionSet.StoreRegisterRange;
        table[3] = XoChipInstructionSet.LoadRegisterRange;
        return table;
    }

    private InstructionHandler[] BuildArithmeticTable()
    {
        var table = new InstructionHandler[16];
        Array.Fill(table, NoOp);
        table[0x0] = Chip8InstructionSet.ExecuteSetRegisterValueFromRegisterIns;
        table[0x1] = Chip8InstructionSet.ExecuteBitwiseOrOnRegistersIns;
        table[0x2] = Chip8InstructionSet.ExecuteBitwiseAndOnRegistersIns;
        table[0x3] = Chip8InstructionSet.ExecuteXorRegisterValueFromRegisterIns;
        table[0x4] = Chip8InstructionSet.ExecuteAddValueToRegisterWithCarryIns;
        table[0x5] = Chip8InstructionSet.ExecuteVxSubVyIns;
        table[0x6] = Chip8InstructionSet.ExecuteShiftRightIns;
        table[0x7] = Chip8InstructionSet.ExecuteVySubVxIns;
        table[0xE] = Chip8InstructionSet.ExecuteShiftLeftIns;
        return table;
    }
}
