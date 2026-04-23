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

    public static void ZeroBase(Chip8Machine machine, int ins)
    {
        // 0NNN (SYS call) — ignore on modern interpreters.
        if ((ins & 0xFF00) != 0x0000) return;
        machine.SystemInsTable[ins & 0x00FF](machine, ins);
    }

    public static void ArithmeticOperation(Chip8Machine machine, int ins)
    {
        machine.ArithmeticTable[ins & 0x000F](machine, ins);
    }

    public static void SkipNextInsIfKeyIsPressedOrReleased(Chip8Machine machine, int ins)
    {
        machine.KeyCheckTable[ins & 0x00FF](machine, ins);
    }

    public static void TimerInstructions(Chip8Machine machine, int ins)
    {
        machine.TimerTable[ins & 0x00FF](machine, ins);
    }

    public static void SkipNextInsIfRegisterValueEqualsRegisterValue(Chip8Machine machine, int ins)
    {
        machine.FiveOpTable[ExtractN(ins)](machine, ins);
    }

    // ---- 0x0*** system ops --------------------------------------------------

    public static void ClearDisplay(Chip8Machine machine, int ins)
    {
        machine.ClearDisplay();
    }

    public static void ReturnFromSubroutine(Chip8Machine machine, int ins)
    {
        var address = machine.PopStack();
        machine.WriteProgramCounter(address);
    }

    // ---- 0x1NNN / 0x2NNN : jump / call --------------------------------------

    public static void JumpToAddress(Chip8Machine machine, int ins)
    {
        var address = ExtractNnn(ins);
        machine.WriteProgramCounter(address);
    }

    public static void CallSubroutine(Chip8Machine machine, int ins)
    {
        var address = ExtractNnn(ins);
        machine.PushStack(machine.ReadProgramCounter());
        machine.WriteProgramCounter(address);
    }

    // ---- 0x3XNN / 0x4XNN / 0x9XY0 : conditional skips ------------------------

    public static void SkipNextInsIfRegisterValueEqualsValue(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        if (machine.ReadGeneralPurposeRegister(x) == nn)
        {
            machine.AdvanceProgramCounter();
        }
    }

    public static void SkipNextInsIfRegisterValueNotEqualsValue(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        if (machine.ReadGeneralPurposeRegister(x) != nn)
        {
            machine.AdvanceProgramCounter();
        }
    }

    public static void SkipNextInsIfRegisterValueNotEqualsRegisterValue(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        if (machine.ReadGeneralPurposeRegister(x) != machine.ReadGeneralPurposeRegister(y))
        {
            machine.AdvanceProgramCounter();
        }
    }

    // ---- 0x5XY0 : SE Vx, Vy (called from FiveOpTable slot 0) ----------------

    public static void ExecuteSkipIfVxEqualsVy(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        if (machine.ReadGeneralPurposeRegister(x) == machine.ReadGeneralPurposeRegister(y))
        {
            machine.AdvanceProgramCounter();
        }
    }

    // ---- 0x6XNN / 0x7XNN ----------------------------------------------------

    public static void SetRegisterValue(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        machine.WriteGeneralPurposeRegister(x, nn);
    }

    public static void AddValueToRegister(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        machine.WriteGeneralPurposeRegister(x, (byte)(machine.ReadGeneralPurposeRegister(x) + nn));
    }

    // ---- 0x8XY* arithmetic/logic --------------------------------------------

    public static void ExecuteSetRegisterValueFromRegisterIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        machine.WriteGeneralPurposeRegister(x, machine.ReadGeneralPurposeRegister(y));
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

    // ---- 0xANNN / 0xBNNN / 0xCXNN ------------------------------------------

    public static void SetIndexRegisterIns(Chip8Machine machine, int ins)
    {
        var nnn = ExtractNnn(ins);
        machine.WriteIndexRegister(nnn);
    }

    // Thin dispatcher (still used by tests).
    public static void JumpWithOffsetIns(Chip8Machine machine, int ins)
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

    public static void GenerateRandomNum(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        var randNum = (byte)Random.Shared.Next(0, 256);
        machine.WriteGeneralPurposeRegister(x, (byte)(randNum & nn));
    }

    // ---- 0xDXYN draw --------------------------------------------------------

    public static void DrawToScreen(Chip8Machine machine, int ins)
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
                SChipInstructionSet.DrawHighResSprite(machine, x, y, planeMask);
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

    internal static void DrawLowResSprite(Chip8Machine machine, int sx, int sy, int height, byte planeMask)
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

    // ---- 0xEX* keyboard skips -----------------------------------------------

    public static void SkipNextInsIfKeyIsPressed(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var key = machine.ReadGeneralPurposeRegister(x);
        if (machine.Input.IsKeyPressed(key))
        {
            machine.AdvanceProgramCounter();
        }
    }

    public static void SkipNextInsIfKeyIsReleased(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var key = machine.ReadGeneralPurposeRegister(x);
        if (!machine.Input.IsKeyPressed(key))
        {
            machine.AdvanceProgramCounter();
        }
    }

    // ---- 0xFX** timer / system ops ------------------------------------------

    public static void ReadDelayTimer(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        machine.WriteGeneralPurposeRegister(x, machine.ReadDelayTimer());
    }

    public static void WaitForKeyPress(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        machine.BeginWaitForKey(x);
    }

    public static void SetDelayTimer(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        machine.WriteDelayTimer(machine.ReadGeneralPurposeRegister(x));
    }

    public static void SetSoundTimer(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        machine.WriteSoundTimer(machine.ReadGeneralPurposeRegister(x));
    }

    public static void AddVxToI(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var i = machine.ReadIndexRegister();
        var vx = machine.ReadGeneralPurposeRegister(x);
        machine.WriteIndexRegister(i + vx);
    }

    public static void LoadLowResFontCharacter(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var value = machine.ReadGeneralPurposeRegister(x);
        machine.WriteIndexRegister((value & 0x0F) * Chip8Machine.LowRestFontCharWidth + Chip8Machine.LowResFontBaseAddress);
    }

    public static void StoreBcdInMemory(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var bcd = machine.ReadGeneralPurposeRegister(x);
        machine.WriteMemory(machine.ReadIndexRegisterWithOffset(0), (byte)(bcd / 100));
        machine.WriteMemory(machine.ReadIndexRegisterWithOffset(1), (byte)(bcd / 10 % 10));
        machine.WriteMemory(machine.ReadIndexRegisterWithOffset(2), (byte)(bcd % 10));
    }

    // FX55/FX65 : store/load V0..Vx. Quirk-sensitive (inc I or keep I).
    // Thin dispatchers (still used by tests). Production dispatch picks a variant at flag-set time.

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
}
