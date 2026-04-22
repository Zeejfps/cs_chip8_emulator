using System.Runtime.CompilerServices;
using static Chip8Emulator.Core.Chip8Disassembler;

namespace Chip8Emulator.Core.Impl;

internal delegate void InstructionHandler(Chip8Machine machine, int ins);

internal static class Cpu
{
    private static void NoOp(Chip8Machine machine, int ins) { }

    public static InstructionHandler[] BuildRootOpcodeTable()
    {
        var table = new InstructionHandler[16];
        table[0x0] = ExecuteZeroBaseIns;
        table[0x1] = ExecuteJumpToAddressIns;
        table[0x2] = ExecuteCallSubroutineIns;
        table[0x3] = ExecuteSkipNextInsIfRegisterValueEqualsValueIns;
        table[0x4] = ExecuteSkipNextInsIfRegisterValueNotEqualsValueIns;
        table[0x5] = ExecuteSkipNextInsIfRegisterValueEqualsRegisterValue;
        table[0x6] = ExecuteSetRegisterValueIns;
        table[0x7] = ExecuteAddValueToRegisterIns;
        table[0x8] = ExecuteArithmeticOperationIns;
        table[0x9] = ExecuteSkipNextInsIfRegisterValueNotEqualsRegisterValue;
        table[0xA] = ExecuteSetIndexRegisterIns;
        table[0xB] = ExecuteJumpWithOffsetIns;
        table[0xC] = ExecuteGenerateRandomNumIns;
        table[0xD] = ExeuteDrawToScreenIns;
        table[0xE] = ExecuteSkipNextInsIfKeyIsPressedOrReleased;
        table[0xF] = ExecuteTimerIns;
        return table;
    }
    
    public static InstructionHandler[] BuildSystemInsTable()
    {
        var table = new InstructionHandler[256];
        Array.Fill(table, NoOp);
        table[0xE0] = ExecuteClearDisplayIns;
        table[0xEE] = ExecuteReturnFromSubroutineIns;
        table[0xFF] = ExecuteEnableHiresModeIns;
        table[0xFE] = ExecuteDisableHiresModeIns;
        table[0xFB] = ExecuteScrollRightIns;
        table[0xFC] = ExecuteScrollLeftIns;
        for (var n = 0; n < 16; n++)
        {
            // 00CN — S-CHIP: scroll display down N rows.
            table[0xC0 + n] = ExecuteScrollDownIns;
            // 00DN — XO-CHIP: scroll display up N rows.
            table[0xD0 + n] = ExecuteScrollUpIns;
        }
        return table;
    }
    
    public static InstructionHandler[] BuildTimerTable()
    {
        var table = new InstructionHandler[256];
        Array.Fill(table, NoOp);
        // F000 NNNN — XO-CHIP long load I with the 16-bit word following the opcode.
        table[0x00] = ExecuteLongLoadIndexRegister;
        // FN01 — XO-CHIP select bitplane mask (N = 0..3).
        table[0x01] = ExecuteSelectPlaneIns;
        // F002 — XO-CHIP copy 16 bytes at [I] into audio pattern buffer.
        table[0x02] = ExecuteLoadAudioPatternIns;
        table[0x07] = ExecuteReadDelayTimer;
        table[0x0A] = ExecuteWaitForKeyPress;
        table[0x15] = ExecuteSetDelayTimer;
        table[0x18] = ExecuteSetSoundTimer;
        table[0x1E] = ExecuteAddVxToI;
        table[0x29] = ExecuteLoadLowResFontCharacter;
        table[0x30] = ExecuteLoadHighResFontCharacter;
        table[0x33] = ExecuteStoreBcdInMemory;
        // FX3A — XO-CHIP set audio playback pitch from Vx.
        table[0x3A] = ExecuteSetPitchIns;
        table[0x55] = ExecuteStoreRegisters;
        table[0x65] = ExecuteLoadRegisters;
        // FX75 / FX85 — SCHIP save/load V0..Vx to persistent user flags.
        table[0x75] = ExecuteSaveFlagsIns;
        table[0x85] = ExecuteLoadFlagsIns;
        return table;
    }

    public static InstructionHandler[] BuildKeyCheckTable()
    {
        var table = new InstructionHandler[256];
        Array.Fill(table, NoOp);
        table[0x9E] = ExecuteSkipNextInsIfKeyIsPressed;
        table[0xA1] = ExecuteSkipNextInsIfKeyIsReleased;
        return table;
    }
    
    public static InstructionHandler[] BuildFiveOpTable()
    {
        var table = new InstructionHandler[16];
        Array.Fill(table, NoOp);
        table[0] = ExecuteSkipIfVxEqualsVy;
        table[2] = ExecuteStoreRegisterRange;
        table[3] = ExecuteLoadRegisterRange;
        return table;
    }
    
    public static InstructionHandler[] BuildArithmeticTable()
    {
        var table = new InstructionHandler[16];
        Array.Fill(table, NoOp);
        table[0x0] = ExecuteSetRegisterValueFromRegisterIns;
        table[0x1] = ExecuteBitwiseOrOnRegistersIns;
        table[0x2] = ExecuteBitwiseAndOnRegistersIns;
        table[0x3] = ExecuteXorRegisterValueFromRegisterIns;
        table[0x4] = ExecuteAddValueToRegisterWithCarryIns;
        table[0x5] = ExecuteVxSubVyIns;
        table[0x6] = ExecuteShiftRightIns;
        table[0x7] = ExecuteVySubVxIns;
        table[0xE] = ExecuteShiftLeftIns;
        return table;
    }
    
    public static void ExecuteScrollRightIns(Chip8Machine machine, int ins)
    {
        machine.ScrollDisplayRight(4);
    }

    public static void ExecuteScrollLeftIns(Chip8Machine machine, int ins)
    {
        machine.ScrollDisplayLeft(4);
    }

    public static void ExecuteScrollDownIns(Chip8Machine machine, int ins)
    {
        machine.ScrollDisplayDown(ins & 0x0F);
    }

    public static void ExecuteScrollUpIns(Chip8Machine machine, int ins)
    {
        machine.ScrollDisplayUp(ins & 0x0F);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void FetchDecodeExecute(Chip8Machine machine)
    {
        var ins = Fetch(machine);
        machine.AdvanceProgramCounter();
        var opcode = (ins & 0xF000) >> 12;
        var execute = machine.RootOpcodeTable[opcode];
        execute(machine, ins);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Fetch(Chip8Machine machine)
    {
        var pc = machine.ReadProgramCounter();
        var ins = machine.ReadMemory(pc) << 8 | machine.ReadMemory(pc + 1);
        return ins;
    }

    public static void ExecuteZeroBaseIns(Chip8Machine machine, int ins)
    {
        // 0NNN (SYS call) — ignore on modern interpreters.
        if ((ins & 0xFF00) != 0x0000) return;
        machine.SystemInsTable[ins & 0x00FF](machine, ins);
    }

    public static void ExecuteEnableHiresModeIns(Chip8Machine machine, int ins)
    {
        machine.EnableHighResMode();
    }

    public static void ExecuteDisableHiresModeIns(Chip8Machine machine, int ins)
    {
        machine.DisableHighResMode();
    }

    public static void ExecuteTimerIns(Chip8Machine machine, int ins)
    {
        machine.TimerTable[ins & 0x00FF](machine, ins);
    }

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

    public static void ExecuteStoreBcdInMemory(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var bcd = machine.ReadGeneralPurposeRegister(x);
        machine.WriteMemory(machine.ReadIndexRegisterWithOffset(0), (byte)(bcd / 100));
        machine.WriteMemory(machine.ReadIndexRegisterWithOffset(1), (byte)(bcd / 10 % 10));
        machine.WriteMemory(machine.ReadIndexRegisterWithOffset(2), (byte)(bcd % 10));
    }

    // Thin dispatcher (still used by tests). Production dispatch picks a variant at flag-set time.
    public static void ExecuteLoadRegisters(Chip8Machine machine, int ins)
    {
        if (machine.LoadStoreIncrementsI) ExecuteLoadRegistersIncIIns(machine, ins);
        else ExecuteLoadRegistersKeepIIns(machine, ins);
    }

    public static void ExecuteLoadRegistersKeepIIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        for (var i = 0; i <= x; i++)
        {
            machine.WriteGeneralPurposeRegister(i, machine.ReadMemory(machine.ReadIndexRegisterWithOffset(i)));
        }
    }

    public static void ExecuteLoadRegistersIncIIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        for (var i = 0; i <= x; i++)
        {
            machine.WriteGeneralPurposeRegister(i, machine.ReadMemory(machine.ReadIndexRegisterWithOffset(i)));
        }
        machine.WriteIndexRegister(machine.ReadIndexRegister() + x + 1);
    }

    // Thin dispatcher (still used by tests).
    public static void ExecuteStoreRegisters(Chip8Machine machine, int ins)
    {
        if (machine.LoadStoreIncrementsI) ExecuteStoreRegistersIncIIns(machine, ins);
        else ExecuteStoreRegistersKeepIIns(machine, ins);
    }

    public static void ExecuteStoreRegistersKeepIIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        for (var i = 0; i <= x; i++)
        {
            machine.WriteMemory(machine.ReadIndexRegisterWithOffset(i), machine.ReadGeneralPurposeRegister(i));
        }
    }

    public static void ExecuteStoreRegistersIncIIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        for (var i = 0; i <= x; i++)
        {
            machine.WriteMemory(machine.ReadIndexRegisterWithOffset(i), machine.ReadGeneralPurposeRegister(i));
        }
        machine.WriteIndexRegister(machine.ReadIndexRegister() + x + 1);
    }

    public static void ExecuteLoadLowResFontCharacter(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var value = machine.ReadGeneralPurposeRegister(x);
        machine.WriteIndexRegister((value & 0x0F) * Chip8Machine.LowRestFontCharWidth + Chip8Machine.LowResFontBaseAddress);
    }

    public static void ExecuteLoadHighResFontCharacter(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var value = machine.ReadGeneralPurposeRegister(x);
        machine.WriteIndexRegister((value & 0x0F) * Chip8Machine.HighRestFontCharWidth + Chip8Machine.HighResFontBaseAddress);
    }

    public static void ExecuteAddVxToI(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var i = machine.ReadIndexRegister();
        var vx = machine.ReadGeneralPurposeRegister(x);
        machine.WriteIndexRegister(i + vx);
    }

    public static void ExecuteWaitForKeyPress(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        machine.BeginWaitForKey(x);
    }

    public static void ExecuteSetSoundTimer(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        machine.WriteSoundTimer(machine.ReadGeneralPurposeRegister(x));
    }

    public static void ExecuteSetDelayTimer(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        machine.WriteDelayTimer(machine.ReadGeneralPurposeRegister(x));
    }

    public static void ExecuteReadDelayTimer(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        machine.WriteGeneralPurposeRegister(x, machine.ReadDelayTimer());
    }

    public static void ExecuteSkipNextInsIfKeyIsPressedOrReleased(Chip8Machine machine, int ins)
    {
        machine.KeyCheckTable[ins & 0x00FF](machine, ins);
    }

    public static void ExecuteSkipNextInsIfKeyIsPressed(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var key = machine.ReadGeneralPurposeRegister(x);
        if (machine.Input.IsKeyPressed(key))
        {
            machine.AdvanceProgramCounter();
        }
    }

    public static void ExecuteSkipNextInsIfKeyIsReleased(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var key = machine.ReadGeneralPurposeRegister(x);
        if (!machine.Input.IsKeyPressed(key))
        {
            machine.AdvanceProgramCounter();
        }
    }

    public static void ExecuteGenerateRandomNumIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        var randNum = (byte)Random.Shared.Next(0, 256);
        machine.WriteGeneralPurposeRegister(x, (byte)(randNum & nn));
    }

    public static void ExecuteSkipNextInsIfRegisterValueNotEqualsRegisterValue(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        if (machine.ReadGeneralPurposeRegister(x) != machine.ReadGeneralPurposeRegister(y))
        {
            machine.AdvanceProgramCounter();
        }
    }

    public static void ExecuteSkipNextInsIfRegisterValueEqualsValueIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        if (machine.ReadGeneralPurposeRegister(x) == nn)
        {
            machine.AdvanceProgramCounter();
        }
    }

    public static void ExecuteSkipNextInsIfRegisterValueNotEqualsValueIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        if (machine.ReadGeneralPurposeRegister(x) != nn)
        {
            machine.AdvanceProgramCounter();
        }
    }

    public static void ExecuteSkipNextInsIfRegisterValueEqualsRegisterValue(Chip8Machine machine, int ins)
    {
        machine.FiveOpTable[ExtractN(ins)](machine, ins);
    }

    private static void ExecuteSkipIfVxEqualsVy(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        if (machine.ReadGeneralPurposeRegister(x) == machine.ReadGeneralPurposeRegister(y))
        {
            machine.AdvanceProgramCounter();
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

    public static void ExecuteCallSubroutineIns(Chip8Machine machine, int ins)
    {
        var address = ExtractNnn(ins);
        machine.PushStack(machine.ReadProgramCounter());
        machine.WriteProgramCounter(address);
    }

    public static void ExeuteDrawToScreenIns(Chip8Machine machine, int ins)
    {
        var display = machine.Display;
        var x = machine.ReadGeneralPurposeRegister(ExtractX(ins)) % display.Width;
        var y = machine.ReadGeneralPurposeRegister(ExtractY(ins)) % display.Height;
        var n = ExtractN(ins);
        var planeMask = (byte)(machine.SelectedPlanes & Display.AllPlanesMask);

        if (planeMask == 0)
        {
            machine.WriteGeneralPurposeRegister(0xF, 0);
            if (machine.DisplayWait) machine.BeginWaitForVBlank();
            return;
        }

        if (n == 0)
        {
            if (display.IsHighRes)
                DrawHighResSprite(machine, x, y, planeMask);
            else
                DrawLowResSprite(machine, x, y, 8, planeMask);
        }
        else
        {
            DrawLowResSprite(machine, x, y, n, planeMask);
        }

        if (machine.DisplayWait)
        {
            machine.BeginWaitForVBlank();
        }
    }

    public static void DrawHighResSprite(Chip8Machine machine, int x, int y, byte planeMask)
    {
        // S-CHIP 1.1 DXY0 hi-res collision semantics (extended for XO-Chip bitplanes):
        // VF = number of sprite rows with at least one collision in any selected plane
        //    + number of sprite rows clipped off the bottom edge (when not wrapping).
        var display = machine.Display;
        var displayPixels = display.Pixels.Span;
        var width = display.Width;
        var height = display.Height;
        var wrap = machine.SpritesWrap;
        var collidedRows = 0;
        var clippedRows = 0;

        // Rows-per-plane: 16 for a single plane, 32 total when both planes selected
        // (first 32 bytes = plane 0, next 32 = plane 1).
        var planeStride = 32;
        var spriteBase = 0;

        Span<bool> rowCollisions = stackalloc bool[16];
        var anyClipped = 0;

        for (var planeBit = 0; planeBit < 2; planeBit++)
        {
            var planeBitMask = (byte)(1 << planeBit);
            if ((planeMask & planeBitMask) == 0) continue;

            for (var i = 0; i < 16; i++)
            {
                var dstY = y + i;
                if (wrap)
                {
                    dstY %= height;
                }
                else if (dstY >= height)
                {
                    anyClipped = Math.Max(anyClipped, 16 - i);
                    break;
                }

                var offset = spriteBase + i * 2;
                var spritePixelsRow = (ushort)(machine.ReadMemory(machine.ReadIndexRegisterWithOffset(offset)) << 8 |
                                               machine.ReadMemory(machine.ReadIndexRegisterWithOffset(offset + 1)));
                for (var bit = 0; bit < 16; bit++)
                {
                    var dstX = x + bit;
                    if (wrap) dstX %= width;
                    else if (dstX >= width) break;

                    var spriteBitOn = ((spritePixelsRow >> (15 - bit)) & 1) != 0;
                    if (!spriteBitOn) continue;

                    var dstIndex = dstY * width + dstX;
                    var before = displayPixels[dstIndex];
                    if ((before & planeBitMask) != 0) rowCollisions[i] = true;
                    displayPixels[dstIndex] = (byte)(before ^ planeBitMask);
                }
            }

            spriteBase += planeStride;
        }

        for (var i = 0; i < 16; i++)
            if (rowCollisions[i]) collidedRows++;
        clippedRows = anyClipped;

        machine.WriteGeneralPurposeRegister(0xF, (byte)(collidedRows + clippedRows));
    }

    private static void DrawLowResSprite(Chip8Machine machine, int sx, int sy, int height, byte planeMask)
    {
        var display = machine.Display;
        var displayPixels = display.Pixels.Span;
        var width = display.Width;
        var displayHeight = display.Height;
        var wrap = machine.SpritesWrap;
        byte collision = 0;

        var spriteBase = 0;
        for (var planeBit = 0; planeBit < 2; planeBit++)
        {
            var planeBitMask = (byte)(1 << planeBit);
            if ((planeMask & planeBitMask) == 0) continue;

            for (var y = 0; y < height; y++)
            {
                var dstY = sy + y;
                if (wrap) dstY %= displayHeight;
                else if (dstY >= displayHeight) break;

                var row = machine.ReadMemory(machine.ReadIndexRegisterWithOffset(spriteBase + y));
                for (var bit = 0; bit < 8; bit++)
                {
                    var dstX = sx + bit;
                    if (wrap) dstX %= width;
                    else if (dstX >= width) break;

                    var spriteBitOn = ((row >> (7 - bit)) & 1) != 0;
                    if (!spriteBitOn) continue;

                    var dstIndex = dstY * width + dstX;
                    var before = displayPixels[dstIndex];
                    if ((before & planeBitMask) != 0) collision = 1;
                    displayPixels[dstIndex] = (byte)(before ^ planeBitMask);
                }
            }

            spriteBase += height;
        }

        machine.WriteGeneralPurposeRegister(0xF, collision);
    }

    public static void ExecuteSelectPlaneIns(Chip8Machine machine, int ins)
    {
        machine.SelectedPlanes = (byte)ExtractX(ins);
    }

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

    public static void ExecuteSaveFlagsIns(Chip8Machine machine, int ins)
    {
        machine.SaveFlags(ExtractX(ins));
    }

    public static void ExecuteLoadFlagsIns(Chip8Machine machine, int ins)
    {
        machine.LoadFlags(ExtractX(ins));
    }

    public static void ExecuteSetRegisterValueIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        machine.WriteGeneralPurposeRegister(x, nn);
    }

    public static void ExecuteArithmeticOperationIns(Chip8Machine machine, int ins)
    {
        machine.ArithmeticTable[ins & 0x000F](machine, ins);
    }

    public static void ExecuteShiftRightIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var value = machine.ReadGeneralPurposeRegister(x);

        if (machine.ShiftUsesVy)
        {
            var y = ExtractY(ins);
            value = machine.ReadGeneralPurposeRegister(y);
        }

        var flag = (byte)(value & 0x1);
        var result = (byte)(value >> 1);
        machine.WriteGeneralPurposeRegister(x, result);
        machine.WriteGeneralPurposeRegister(0xF, flag);
        if (machine.VfResultWrittenLast) machine.WriteGeneralPurposeRegister(x, result);
    }

    public static void ExecuteShiftLeftIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var value = machine.ReadGeneralPurposeRegister(x);

        if (machine.ShiftUsesVy)
        {
            var y = ExtractY(ins);
            value = machine.ReadGeneralPurposeRegister(y);
        }

        var flag = (byte)((value >> 7) & 0x1);
        var result = (byte)(value << 1);
        machine.WriteGeneralPurposeRegister(x, result);
        machine.WriteGeneralPurposeRegister(0xF, flag);
        if (machine.VfResultWrittenLast) machine.WriteGeneralPurposeRegister(x, result);
    }

    public static void ExecuteVxSubVyIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        var minuend = machine.ReadGeneralPurposeRegister(x);
        var subtrahend = machine.ReadGeneralPurposeRegister(y);
        var flag = (byte)(minuend >= subtrahend ? 1 : 0);
        var result = (byte)(minuend - subtrahend);
        machine.WriteGeneralPurposeRegister(x, result);
        machine.WriteGeneralPurposeRegister(0xF, flag);
        if (machine.VfResultWrittenLast) machine.WriteGeneralPurposeRegister(x, result);
    }

    public static void ExecuteVySubVxIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        // NOTE(Zee): y first
        var minuend = machine.ReadGeneralPurposeRegister(y);
        var subtrahend = machine.ReadGeneralPurposeRegister(x);
        var flag = (byte)(minuend >= subtrahend ? 1 : 0);
        var result = (byte)(minuend - subtrahend);
        machine.WriteGeneralPurposeRegister(x, result);
        machine.WriteGeneralPurposeRegister(0xF, flag);
        if (machine.VfResultWrittenLast) machine.WriteGeneralPurposeRegister(x, result);
    }

    public static void ExecuteAddValueToRegisterWithCarryIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        var sum = machine.ReadGeneralPurposeRegister(x) + machine.ReadGeneralPurposeRegister(y);
        var carry = (byte)(sum > 0xFF ? 1 : 0);
        var result = (byte)sum;
        machine.WriteGeneralPurposeRegister(x, result);
        machine.WriteGeneralPurposeRegister(0xF, carry);
        if (machine.VfResultWrittenLast) machine.WriteGeneralPurposeRegister(x, result);
    }

    // Thin dispatcher (still used by tests).
    public static void ExecuteBitwiseOrOnRegistersIns(Chip8Machine machine, int ins)
    {
        if (machine.LogicResetsVf) ExecuteBitwiseOrResetVfIns(machine, ins);
        else ExecuteBitwiseOrPreserveVfIns(machine, ins);
    }

    public static void ExecuteBitwiseOrPreserveVfIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        machine.WriteGeneralPurposeRegister(x, (byte)(machine.ReadGeneralPurposeRegister(x) | machine.ReadGeneralPurposeRegister(y)));
    }

    public static void ExecuteBitwiseOrResetVfIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        machine.WriteGeneralPurposeRegister(x, (byte)(machine.ReadGeneralPurposeRegister(x) | machine.ReadGeneralPurposeRegister(y)));
        machine.WriteGeneralPurposeRegister(0xF, 0);
    }

    // Thin dispatcher (still used by tests).
    public static void ExecuteBitwiseAndOnRegistersIns(Chip8Machine machine, int ins)
    {
        if (machine.LogicResetsVf) ExecuteBitwiseAndResetVfIns(machine, ins);
        else ExecuteBitwiseAndPreserveVfIns(machine, ins);
    }

    public static void ExecuteBitwiseAndPreserveVfIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        machine.WriteGeneralPurposeRegister(x, (byte)(machine.ReadGeneralPurposeRegister(x) & machine.ReadGeneralPurposeRegister(y)));
    }

    public static void ExecuteBitwiseAndResetVfIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        machine.WriteGeneralPurposeRegister(x, (byte)(machine.ReadGeneralPurposeRegister(x) & machine.ReadGeneralPurposeRegister(y)));
        machine.WriteGeneralPurposeRegister(0xF, 0);
    }

    public static void ExecuteSetRegisterValueFromRegisterIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        machine.WriteGeneralPurposeRegister(x, machine.ReadGeneralPurposeRegister(y));
    }

    // Thin dispatcher (still used by tests).
    public static void ExecuteXorRegisterValueFromRegisterIns(Chip8Machine machine, int ins)
    {
        if (machine.LogicResetsVf) ExecuteXorResetVfIns(machine, ins);
        else ExecuteXorPreserveVfIns(machine, ins);
    }

    public static void ExecuteXorPreserveVfIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        machine.WriteGeneralPurposeRegister(x, (byte)(machine.ReadGeneralPurposeRegister(x) ^ machine.ReadGeneralPurposeRegister(y)));
    }

    public static void ExecuteXorResetVfIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        machine.WriteGeneralPurposeRegister(x, (byte)(machine.ReadGeneralPurposeRegister(x) ^ machine.ReadGeneralPurposeRegister(y)));
        machine.WriteGeneralPurposeRegister(0xF, 0);
    }

    public static void ExecuteAddValueToRegisterIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        machine.WriteGeneralPurposeRegister(x, (byte)(machine.ReadGeneralPurposeRegister(x) + nn));
    }

    public static void ExecuteSetIndexRegisterIns(Chip8Machine machine, int ins)
    {
        var nnn = ExtractNnn(ins);
        machine.WriteIndexRegister(nnn);
    }

    public static void ExecuteJumpToAddressIns(Chip8Machine machine, int ins)
    {
        var address = ExtractNnn(ins);
        machine.WriteProgramCounter(address);
    }

    // Thin dispatcher (still used by tests).
    public static void ExecuteJumpWithOffsetIns(Chip8Machine machine, int ins)
    {
        if (machine.JumpUsesVx) ExecuteJumpWithVxOffsetIns(machine, ins);
        else ExecuteJumpWithV0OffsetIns(machine, ins);
    }

    public static void ExecuteJumpWithV0OffsetIns(Chip8Machine machine, int ins)
    {
        var address = ExtractNnn(ins);
        machine.WriteProgramCounter(address + machine.ReadGeneralPurposeRegister(0));
    }

    public static void ExecuteJumpWithVxOffsetIns(Chip8Machine machine, int ins)
    {
        var address = ExtractNnn(ins);
        var x = ExtractX(ins);
        machine.WriteProgramCounter(address + machine.ReadGeneralPurposeRegister(x));
    }

    public static void ExecuteReturnFromSubroutineIns(Chip8Machine machine, int ins)
    {
        var address = machine.PopStack();
        machine.WriteProgramCounter(address);
    }

    public static void ExecuteClearDisplayIns(Chip8Machine machine, int ins)
    {
        machine.ClearDisplay();
    }
}
