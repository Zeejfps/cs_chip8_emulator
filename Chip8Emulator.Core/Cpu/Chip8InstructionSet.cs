using static Chip8Emulator.Core.Chip8Disassembler;

namespace Chip8Emulator.Core.Cpu;

// Base CHIP-8 opcode handlers plus the quirk-variant handlers for the ambiguous
// CHIP-8 instructions (OR/AND/XOR VF reset, SHR/SHL Vx-vs-Vy, BNNN jump, FX55/FX65
// inc-I). The quirk variants live here because they are alternate interpretations
// of CHIP-8 ops, not later-CPU additions; Chip8Machine.Apply* methods pick between
// them at flag-set time.
internal static class Chip8InstructionSet
{
    // ---- Sub-table dispatchers (CHIP-8 fan-outs) ----------------------------

    public static void SkipNextInsIfKeyIsPressedOrReleased(ICpu cpu, int ins)
    {
        cpu.DispatchKeyCheckInstruction(ins);
    }

    public static void SkipNextInsIfRegisterValueEqualsRegisterValue(ICpu cpu, int ins)
    {
        cpu.DispatchFiveOpInstruction(ins);
    }

    // ---- 0x0*** system ops --------------------------------------------------

    public static void ClearDisplay(ICpu cpu, int ins)
    {
        cpu.ClearDisplay();
    }

    public static void ReturnFromSubroutine(ICpu cpu, int ins)
    {
        var address = cpu.Stack.Pop();
        cpu.WriteProgramCounter(address);
    }

    // ---- 0x1NNN / 0x2NNN : jump / call --------------------------------------

    public static void JumpToAddress(ICpu cpu, int ins)
    {
        var address = ExtractNnn(ins);
        cpu.WriteProgramCounter(address);
    }

    public static void CallSubroutine(ICpu cpu, int ins)
    {
        var address = ExtractNnn(ins);
        cpu.Stack.Push(cpu.ReadProgramCounter());
        cpu.WriteProgramCounter(address);
    }

    // ---- 0x3XNN / 0x4XNN / 0x9XY0 : conditional skips ------------------------

    public static void SkipNextInsIfRegisterValueEqualsValue(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        if (cpu.ReadGeneralPurposeRegister(x) == nn)
        {
            cpu.AdvanceProgramCounter();
        }
    }

    public static void SkipNextInsIfRegisterValueNotEqualsValue(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        if (cpu.ReadGeneralPurposeRegister(x) != nn)
        {
            cpu.AdvanceProgramCounter();
        }
    }

    public static void SkipNextInsIfRegisterValueNotEqualsRegisterValue(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        if (cpu.ReadGeneralPurposeRegister(x) != cpu.ReadGeneralPurposeRegister(y))
        {
            cpu.AdvanceProgramCounter();
        }
    }

    // ---- 0x5XY0 : SE Vx, Vy (called from FiveOpTable slot 0) ----------------

    public static void SkipIfVxEqualsVy(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        if (cpu.ReadGeneralPurposeRegister(x) == cpu.ReadGeneralPurposeRegister(y))
        {
            cpu.AdvanceProgramCounter();
        }
    }

    // ---- 0x6XNN / 0x7XNN ----------------------------------------------------

    public static void SetRegisterValue(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        cpu.WriteGeneralPurposeRegister(x, nn);
    }

    public static void AddValueToRegister(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        cpu.WriteGeneralPurposeRegister(x, (byte)(cpu.ReadGeneralPurposeRegister(x) + nn));
    }

    // ---- 0x8XY* arithmetic/logic --------------------------------------------

    public static void ExecuteSetRegisterValueFromRegisterIns(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        cpu.WriteGeneralPurposeRegister(x, cpu.ReadGeneralPurposeRegister(y));
    }

    // Thin dispatcher (still used by tests).
    public static void ExecuteBitwiseOrOnRegistersIns(ICpu cpu, int ins)
    {
        if (cpu.LogicResetsVf) ExecuteBitwiseOrResetVfIns(cpu, ins);
        else ExecuteBitwiseOrPreserveVfIns(cpu, ins);
    }

    public static void ExecuteBitwiseOrPreserveVfIns(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        cpu.WriteGeneralPurposeRegister(x, (byte)(cpu.ReadGeneralPurposeRegister(x) | cpu.ReadGeneralPurposeRegister(y)));
    }

    public static void ExecuteBitwiseOrResetVfIns(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        cpu.WriteGeneralPurposeRegister(x, (byte)(cpu.ReadGeneralPurposeRegister(x) | cpu.ReadGeneralPurposeRegister(y)));
        cpu.WriteGeneralPurposeRegister(0xF, 0);
    }

    // Thin dispatcher (still used by tests).
    public static void ExecuteBitwiseAndOnRegistersIns(ICpu cpu, int ins)
    {
        if (cpu.LogicResetsVf) ExecuteBitwiseAndResetVfIns(cpu, ins);
        else ExecuteBitwiseAndPreserveVfIns(cpu, ins);
    }

    public static void ExecuteBitwiseAndPreserveVfIns(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        cpu.WriteGeneralPurposeRegister(x, (byte)(cpu.ReadGeneralPurposeRegister(x) & cpu.ReadGeneralPurposeRegister(y)));
    }

    public static void ExecuteBitwiseAndResetVfIns(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        cpu.WriteGeneralPurposeRegister(x, (byte)(cpu.ReadGeneralPurposeRegister(x) & cpu.ReadGeneralPurposeRegister(y)));
        cpu.WriteGeneralPurposeRegister(0xF, 0);
    }

    // Thin dispatcher (still used by tests).
    public static void ExecuteXorRegisterValueFromRegisterIns(ICpu cpu, int ins)
    {
        if (cpu.LogicResetsVf) ExecuteXorResetVfIns(cpu, ins);
        else ExecuteXorPreserveVfIns(cpu, ins);
    }

    public static void ExecuteXorPreserveVfIns(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        cpu.WriteGeneralPurposeRegister(x, (byte)(cpu.ReadGeneralPurposeRegister(x) ^ cpu.ReadGeneralPurposeRegister(y)));
    }

    public static void ExecuteXorResetVfIns(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        cpu.WriteGeneralPurposeRegister(x, (byte)(cpu.ReadGeneralPurposeRegister(x) ^ cpu.ReadGeneralPurposeRegister(y)));
        cpu.WriteGeneralPurposeRegister(0xF, 0);
    }

    public static void ExecuteAddValueToRegisterWithCarryIns(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        var sum = cpu.ReadGeneralPurposeRegister(x) + cpu.ReadGeneralPurposeRegister(y);
        var carry = (byte)(sum > 0xFF ? 1 : 0);
        var result = (byte)sum;
        cpu.WriteGeneralPurposeRegister(x, result);
        cpu.WriteGeneralPurposeRegister(0xF, carry);
        if (cpu.VfResultWrittenLast) cpu.WriteGeneralPurposeRegister(x, result);
    }

    public static void ExecuteVxSubVyIns(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        var minuend = cpu.ReadGeneralPurposeRegister(x);
        var subtrahend = cpu.ReadGeneralPurposeRegister(y);
        var flag = (byte)(minuend >= subtrahend ? 1 : 0);
        var result = (byte)(minuend - subtrahend);
        cpu.WriteGeneralPurposeRegister(x, result);
        cpu.WriteGeneralPurposeRegister(0xF, flag);
        if (cpu.VfResultWrittenLast) cpu.WriteGeneralPurposeRegister(x, result);
    }

    public static void ExecuteVySubVxIns(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        // NOTE(Zee): y first
        var minuend = cpu.ReadGeneralPurposeRegister(y);
        var subtrahend = cpu.ReadGeneralPurposeRegister(x);
        var flag = (byte)(minuend >= subtrahend ? 1 : 0);
        var result = (byte)(minuend - subtrahend);
        cpu.WriteGeneralPurposeRegister(x, result);
        cpu.WriteGeneralPurposeRegister(0xF, flag);
        if (cpu.VfResultWrittenLast) cpu.WriteGeneralPurposeRegister(x, result);
    }

    public static void ExecuteShiftRightIns(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var value = cpu.ReadGeneralPurposeRegister(x);

        if (cpu.ShiftUsesVy)
        {
            var y = ExtractY(ins);
            value = cpu.ReadGeneralPurposeRegister(y);
        }

        var flag = (byte)(value & 0x1);
        var result = (byte)(value >> 1);
        cpu.WriteGeneralPurposeRegister(x, result);
        cpu.WriteGeneralPurposeRegister(0xF, flag);
        if (cpu.VfResultWrittenLast) cpu.WriteGeneralPurposeRegister(x, result);
    }

    public static void ExecuteShiftLeftIns(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var value = cpu.ReadGeneralPurposeRegister(x);

        if (cpu.ShiftUsesVy)
        {
            var y = ExtractY(ins);
            value = cpu.ReadGeneralPurposeRegister(y);
        }

        var flag = (byte)((value >> 7) & 0x1);
        var result = (byte)(value << 1);
        cpu.WriteGeneralPurposeRegister(x, result);
        cpu.WriteGeneralPurposeRegister(0xF, flag);
        if (cpu.VfResultWrittenLast) cpu.WriteGeneralPurposeRegister(x, result);
    }

    // ---- 0xANNN / 0xBNNN / 0xCXNN ------------------------------------------

    public static void SetIndexRegisterIns(ICpu cpu, int ins)
    {
        var nnn = ExtractNnn(ins);
        cpu.WriteIndexRegister(nnn);
    }

    // Thin dispatcher (still used by tests).
    public static void JumpWithOffsetIns(ICpu cpu, int ins)
    {
        if (cpu.JumpUsesVx) ExecuteJumpWithVxOffsetIns(cpu, ins);
        else ExecuteJumpWithV0OffsetIns(cpu, ins);
    }

    public static void ExecuteJumpWithV0OffsetIns(ICpu cpu, int ins)
    {
        var address = ExtractNnn(ins);
        cpu.WriteProgramCounter(address + cpu.ReadGeneralPurposeRegister(0));
    }

    public static void ExecuteJumpWithVxOffsetIns(ICpu cpu, int ins)
    {
        var address = ExtractNnn(ins);
        var x = ExtractX(ins);
        cpu.WriteProgramCounter(address + cpu.ReadGeneralPurposeRegister(x));
    }

    public static void GenerateRandomNum(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        var randNum = (byte)Random.Shared.Next(0, 256);
        cpu.WriteGeneralPurposeRegister(x, (byte)(randNum & nn));
    }

    // ---- 0xDXYN draw --------------------------------------------------------

    public static void DrawToScreen(ICpu cpu, int ins)
    {
        var display = cpu.Display;
        var x = cpu.ReadGeneralPurposeRegister(ExtractX(ins)) % display.Width;
        var y = cpu.ReadGeneralPurposeRegister(ExtractY(ins)) % display.Height;
        var n = ExtractN(ins);
        var planeMask = (byte)(cpu.SelectedPlanes & Display.AllPlanesMask);

        if (planeMask == 0)
        {
            cpu.WriteGeneralPurposeRegister(0xF, 0);
            if (cpu.DisplayWait) cpu.BeginWaitForVBlank();
            return;
        }

        if (n == 0)
        {
            if (display.IsHighRes)
                SChipInstructionSet.DrawHighResSprite(cpu, x, y, planeMask);
            else
                DrawLowResSprite(cpu, x, y, 8, planeMask);
        }
        else
        {
            DrawLowResSprite(cpu, x, y, n, planeMask);
        }

        if (cpu.DisplayWait)
        {
            cpu.BeginWaitForVBlank();
        }
    }

    internal static void DrawLowResSprite(ICpu cpu, int sx, int sy, int height, byte planeMask)
    {
        var display = cpu.Display;
        var displayPixels = display.Pixels.Span;
        var width = display.Width;
        var displayHeight = display.Height;
        var wrap = cpu.SpritesWrap;
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

                var row = cpu.Memory.Read(cpu.ReadIndexRegisterWithOffset(spriteBase + y));
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

        cpu.WriteGeneralPurposeRegister(0xF, collision);
    }

    // ---- 0xEX* keyboard skips -----------------------------------------------

    public static void SkipNextInsIfKeyIsPressed(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var key = cpu.ReadGeneralPurposeRegister(x);
        if (cpu.Input.IsKeyPressed(key))
        {
            cpu.AdvanceProgramCounter();
        }
    }

    public static void SkipNextInsIfKeyIsReleased(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var key = cpu.ReadGeneralPurposeRegister(x);
        if (!cpu.Input.IsKeyPressed(key))
        {
            cpu.AdvanceProgramCounter();
        }
    }

    // ---- 0xFX** timer / system ops ------------------------------------------

    public static void ReadDelayTimer(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        cpu.WriteGeneralPurposeRegister(x, cpu.ReadDelayTimer());
    }

    public static void WaitForKeyPressAndRelease(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        cpu.BeginWaitForKey(x);
    }

    public static void SetDelayTimer(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        cpu.WriteDelayTimer(cpu.ReadGeneralPurposeRegister(x));
    }

    public static void SetSoundTimer(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        cpu.WriteSoundTimer(cpu.ReadGeneralPurposeRegister(x));
    }

    public static void AddVxToI(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var i = cpu.ReadIndexRegister();
        var vx = cpu.ReadGeneralPurposeRegister(x);
        cpu.WriteIndexRegister(i + vx);
    }

    public static void LoadLowResFontCharacter(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var value = cpu.ReadGeneralPurposeRegister(x);
        cpu.WriteIndexRegister((value & 0x0F) * Chip8Machine.LowRestFontCharWidth + Chip8Machine.LowResFontBaseAddress);
    }

    public static void StoreBcdInMemory(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var bcd = cpu.ReadGeneralPurposeRegister(x);
        cpu.Memory.Write(cpu.ReadIndexRegisterWithOffset(0), (byte)(bcd / 100));
        cpu.Memory.Write(cpu.ReadIndexRegisterWithOffset(1), (byte)(bcd / 10 % 10));
        cpu.Memory.Write(cpu.ReadIndexRegisterWithOffset(2), (byte)(bcd % 10));
    }

    // FX55/FX65 : store/load V0..Vx. Quirk-sensitive (inc I or keep I).
    // Thin dispatchers (still used by tests). Production dispatch picks a variant at flag-set time.

    public static void LoadRegisters(ICpu cpu, int ins)
    {
        if (cpu.LoadStoreIncrementsI) ExecuteLoadRegistersIncIIns(cpu, ins);
        else ExecuteLoadRegistersKeepIIns(cpu, ins);
    }

    public static void ExecuteLoadRegistersKeepIIns(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        for (var i = 0; i <= x; i++)
        {
            cpu.WriteGeneralPurposeRegister(i, cpu.Memory.Read(cpu.ReadIndexRegisterWithOffset(i)));
        }
    }

    public static void ExecuteLoadRegistersIncIIns(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        for (var i = 0; i <= x; i++)
        {
            cpu.WriteGeneralPurposeRegister(i, cpu.Memory.Read(cpu.ReadIndexRegisterWithOffset(i)));
        }
        cpu.WriteIndexRegister(cpu.ReadIndexRegister() + x + 1);
    }

    public static void StoreRegisters(ICpu cpu, int ins)
    {
        if (cpu.LoadStoreIncrementsI) ExecuteStoreRegistersIncIIns(cpu, ins);
        else ExecuteStoreRegistersKeepIIns(cpu, ins);
    }

    public static void ExecuteStoreRegistersKeepIIns(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        for (var i = 0; i <= x; i++)
        {
            cpu.Memory.Write(cpu.ReadIndexRegisterWithOffset(i), cpu.ReadGeneralPurposeRegister(i));
        }
    }

    public static void ExecuteStoreRegistersIncIIns(ICpu cpu, int ins)
    {
        var x = ExtractX(ins);
        for (var i = 0; i <= x; i++)
        {
            cpu.Memory.Write(cpu.ReadIndexRegisterWithOffset(i), cpu.ReadGeneralPurposeRegister(i));
        }
        cpu.WriteIndexRegister(cpu.ReadIndexRegister() + x + 1);
    }
}
