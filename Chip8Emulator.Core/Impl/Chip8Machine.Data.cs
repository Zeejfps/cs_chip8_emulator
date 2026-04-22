namespace Chip8Emulator.Core.Impl;

internal delegate void InstructionHandler(Chip8Machine machine, int ins);

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
    
    private static void NoOp(Chip8Machine machine, int ins) { }

    private InstructionHandler[] BuildRootOpcodeTable()
    {
        var table = new InstructionHandler[16];
        table[0x0] = Chip8Cpu.ExecuteZeroBaseIns;
        table[0x1] = Chip8Cpu.ExecuteJumpToAddressIns;
        table[0x2] = Chip8Cpu.ExecuteCallSubroutineIns;
        table[0x3] = Chip8Cpu.ExecuteSkipNextInsIfRegisterValueEqualsValueIns;
        table[0x4] = Chip8Cpu.ExecuteSkipNextInsIfRegisterValueNotEqualsValueIns;
        table[0x5] = Chip8Cpu.ExecuteSkipNextInsIfRegisterValueEqualsRegisterValue;
        table[0x6] = Chip8Cpu.ExecuteSetRegisterValueIns;
        table[0x7] = Chip8Cpu.ExecuteAddValueToRegisterIns;
        table[0x8] = Chip8Cpu.ExecuteArithmeticOperationIns;
        table[0x9] = Chip8Cpu.ExecuteSkipNextInsIfRegisterValueNotEqualsRegisterValue;
        table[0xA] = Chip8Cpu.ExecuteSetIndexRegisterIns;
        table[0xB] = Chip8Cpu.ExecuteJumpWithOffsetIns;
        table[0xC] = Chip8Cpu.ExecuteGenerateRandomNumIns;
        table[0xD] = Chip8Cpu.ExeuteDrawToScreenIns;
        table[0xE] = Chip8Cpu.ExecuteSkipNextInsIfKeyIsPressedOrReleased;
        table[0xF] = Chip8Cpu.ExecuteTimerIns;
        return table;
    }

    private InstructionHandler[] BuildSystemInsTable()
    {
        var table = new InstructionHandler[256];
        Array.Fill(table, NoOp);
        table[0xE0] = Chip8Cpu.ExecuteClearDisplayIns;
        table[0xEE] = Chip8Cpu.ExecuteReturnFromSubroutineIns;
        table[0xFF] = SChipCpu.ExecuteEnableHiresModeIns;
        table[0xFE] = SChipCpu.ExecuteDisableHiresModeIns;
        table[0xFB] = SChipCpu.ExecuteScrollRightIns;
        table[0xFC] = SChipCpu.ExecuteScrollLeftIns;
        for (var n = 0; n < 16; n++)
        {
            // 00CN — S-CHIP: scroll display down N rows.
            table[0xC0 + n] = SChipCpu.ExecuteScrollDownIns;
            // 00DN — XO-CHIP: scroll display up N rows.
            table[0xD0 + n] = XoChipCpu.ExecuteScrollUpIns;
        }
        return table;
    }

    private InstructionHandler[] BuildTimerTable()
    {
        var table = new InstructionHandler[256];
        Array.Fill(table, NoOp);
        // F000 NNNN — XO-CHIP long load I with the 16-bit word following the opcode.
        table[0x00] = XoChipCpu.ExecuteLongLoadIndexRegister;
        // FN01 — XO-CHIP select bitplane mask (N = 0..3).
        table[0x01] = XoChipCpu.ExecuteSelectPlaneIns;
        // F002 — XO-CHIP copy 16 bytes at [I] into audio pattern buffer.
        table[0x02] = XoChipCpu.ExecuteLoadAudioPatternIns;
        table[0x07] = Chip8Cpu.ExecuteReadDelayTimer;
        table[0x0A] = Chip8Cpu.ExecuteWaitForKeyPress;
        table[0x15] = Chip8Cpu.ExecuteSetDelayTimer;
        table[0x18] = Chip8Cpu.ExecuteSetSoundTimer;
        table[0x1E] = Chip8Cpu.ExecuteAddVxToI;
        table[0x29] = Chip8Cpu.ExecuteLoadLowResFontCharacter;
        table[0x30] = SChipCpu.ExecuteLoadHighResFontCharacter;
        table[0x33] = Chip8Cpu.ExecuteStoreBcdInMemory;
        // FX3A — XO-CHIP set audio playback pitch from Vx.
        table[0x3A] = XoChipCpu.ExecuteSetPitchIns;
        table[0x55] = Chip8Cpu.ExecuteStoreRegisters;
        table[0x65] = Chip8Cpu.ExecuteLoadRegisters;
        // FX75 / FX85 — SCHIP save/load V0..Vx to persistent user flags.
        table[0x75] = SChipCpu.ExecuteSaveFlagsIns;
        table[0x85] = SChipCpu.ExecuteLoadFlagsIns;
        return table;
    }

    private InstructionHandler[] BuildKeyCheckTable()
    {
        var table = new InstructionHandler[256];
        Array.Fill(table, NoOp);
        table[0x9E] = Chip8Cpu.ExecuteSkipNextInsIfKeyIsPressed;
        table[0xA1] = Chip8Cpu.ExecuteSkipNextInsIfKeyIsReleased;
        return table;
    }

    private InstructionHandler[] BuildFiveOpTable()
    {
        var table = new InstructionHandler[16];
        Array.Fill(table, NoOp);
        table[0] = Chip8Cpu.ExecuteSkipIfVxEqualsVy;
        table[2] = XoChipCpu.ExecuteStoreRegisterRange;
        table[3] = XoChipCpu.ExecuteLoadRegisterRange;
        return table;
    }

    private InstructionHandler[] BuildArithmeticTable()
    {
        var table = new InstructionHandler[16];
        Array.Fill(table, NoOp);
        table[0x0] = Chip8Cpu.ExecuteSetRegisterValueFromRegisterIns;
        table[0x1] = Chip8Cpu.ExecuteBitwiseOrOnRegistersIns;
        table[0x2] = Chip8Cpu.ExecuteBitwiseAndOnRegistersIns;
        table[0x3] = Chip8Cpu.ExecuteXorRegisterValueFromRegisterIns;
        table[0x4] = Chip8Cpu.ExecuteAddValueToRegisterWithCarryIns;
        table[0x5] = Chip8Cpu.ExecuteVxSubVyIns;
        table[0x6] = Chip8Cpu.ExecuteShiftRightIns;
        table[0x7] = Chip8Cpu.ExecuteVySubVxIns;
        table[0xE] = Chip8Cpu.ExecuteShiftLeftIns;
        return table;
    }
}
