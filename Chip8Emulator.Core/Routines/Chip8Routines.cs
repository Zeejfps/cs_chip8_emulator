using static Chip8Emulator.Core.Chip8Disassembler;

namespace Chip8Emulator.Core.Routines;

// Base CHIP-8 opcode handlers plus the quirk-variant handlers for the ambiguous
// CHIP-8 instructions (OR/AND/XOR VF reset, SHR/SHL Vx-vs-Vy, BNNN jump, FX55/FX65
// inc-I). The quirk variants live here because they are alternate interpretations
// of CHIP-8 ops, not later-CPU additions; Chip8Machine.Apply* methods pick between
// them at flag-set time.
internal static class Chip8Routines
{
    // ---- 0x0*** system ops --------------------------------------------------

    public static void ClearDisplay(EmulatedCpu cpu, int ins)
    {
        cpu.Display.Clear();
    }

    public static void ReturnFromSubroutine(EmulatedCpu cpu, int ins)
    {
        var address = cpu.Stack.Pop();
        cpu.WriteProgramCounter(address);
    }

    // ---- 0x1NNN / 0x2NNN : jump / call --------------------------------------

    public static void JumpToAddress(EmulatedCpu cpu, int ins)
    {
        var address = ExtractNnn(ins);
        cpu.WriteProgramCounter(address);
    }

    public static void CallSubroutine(EmulatedCpu cpu, int ins)
    {
        var address = ExtractNnn(ins);
        cpu.Stack.Push(cpu.ReadProgramCounter());
        cpu.WriteProgramCounter(address);
    }

    // ---- 0x3XNN / 0x4XNN / 0x9XY0 : conditional skips ------------------------

    public static void SkipNextInsIfRegisterValueEqualsValue(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        if (cpu.Registers.ReadV(x) == nn)
        {
            cpu.AdvanceProgramCounter();
        }
    }

    public static void SkipNextInsIfRegisterValueNotEqualsValue(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        if (cpu.Registers.ReadV(x) != nn)
        {
            cpu.AdvanceProgramCounter();
        }
    }

    public static void SkipNextInsIfRegisterValueNotEqualsRegisterValue(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        if (cpu.Registers.ReadV(x) != cpu.Registers.ReadV(y))
        {
            cpu.AdvanceProgramCounter();
        }
    }

    // ---- 0x5XY0 : SE Vx, Vy (called from FiveOpTable slot 0) ----------------

    public static void SkipIfVxEqualsVy(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        if (cpu.Registers.ReadV(x) == cpu.Registers.ReadV(y))
        {
            cpu.AdvanceProgramCounter();
        }
    }

    // ---- 0x6XNN / 0x7XNN ----------------------------------------------------

    public static void SetRegisterValue(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        cpu.Registers.WriteV(x, nn);
    }

    public static void AddValueToRegister(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        cpu.Registers.WriteV(x, (byte)(cpu.Registers.ReadV(x) + nn));
    }

    // ---- 0x8XY* arithmetic/logic --------------------------------------------

    public static void SetRegisterValueFromRegister(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        cpu.Registers.WriteV(x, cpu.Registers.ReadV(y));
    }

    // Thin dispatcher (still used by tests).
    public static void BitwiseOrOnRegisters(EmulatedCpu cpu, int ins)
    {
        if (cpu.LogicResetsVf) ExecuteBitwiseOrResetVfIns(cpu, ins);
        else ExecuteBitwiseOrPreserveVfIns(cpu, ins);
    }

    public static void ExecuteBitwiseOrPreserveVfIns(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        cpu.Registers.WriteV(x, (byte)(cpu.Registers.ReadV(x) | cpu.Registers.ReadV(y)));
    }

    public static void ExecuteBitwiseOrResetVfIns(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        cpu.Registers.WriteV(x, (byte)(cpu.Registers.ReadV(x) | cpu.Registers.ReadV(y)));
        cpu.Registers.WriteV(0xF, 0);
    }

    // Thin dispatcher (still used by tests).
    public static void BitwiseAndOnRegisters(EmulatedCpu cpu, int ins)
    {
        if (cpu.LogicResetsVf) ExecuteBitwiseAndResetVfIns(cpu, ins);
        else ExecuteBitwiseAndPreserveVfIns(cpu, ins);
    }

    public static void ExecuteBitwiseAndPreserveVfIns(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        cpu.Registers.WriteV(x, (byte)(cpu.Registers.ReadV(x) & cpu.Registers.ReadV(y)));
    }

    public static void ExecuteBitwiseAndResetVfIns(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        cpu.Registers.WriteV(x, (byte)(cpu.Registers.ReadV(x) & cpu.Registers.ReadV(y)));
        cpu.Registers.WriteV(0xF, 0);
    }

    // Thin dispatcher (still used by tests).
    public static void XorRegisterValueFromRegister(EmulatedCpu cpu, int ins)
    {
        if (cpu.LogicResetsVf) ExecuteXorResetVfIns(cpu, ins);
        else ExecuteXorPreserveVfIns(cpu, ins);
    }

    public static void ExecuteXorPreserveVfIns(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        cpu.Registers.WriteV(x, (byte)(cpu.Registers.ReadV(x) ^ cpu.Registers.ReadV(y)));
    }

    public static void ExecuteXorResetVfIns(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        cpu.Registers.WriteV(x, (byte)(cpu.Registers.ReadV(x) ^ cpu.Registers.ReadV(y)));
        cpu.Registers.WriteV(0xF, 0);
    }

    public static void AddValueToRegisterWithCarry(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        var sum = cpu.Registers.ReadV(x) + cpu.Registers.ReadV(y);
        var carry = (byte)(sum > 0xFF ? 1 : 0);
        var result = (byte)sum;
        cpu.Registers.WriteV(x, result);
        cpu.Registers.WriteV(0xF, carry);
        if (cpu.VfResultWrittenLast) cpu.Registers.WriteV(x, result);
    }

    public static void VxSubVy(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        var minuend = cpu.Registers.ReadV(x);
        var subtrahend = cpu.Registers.ReadV(y);
        var flag = (byte)(minuend >= subtrahend ? 1 : 0);
        var result = (byte)(minuend - subtrahend);
        cpu.Registers.WriteV(x, result);
        cpu.Registers.WriteV(0xF, flag);
        if (cpu.VfResultWrittenLast) cpu.Registers.WriteV(x, result);
    }

    public static void VySubVx(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        // NOTE(Zee): y first
        var minuend = cpu.Registers.ReadV(y);
        var subtrahend = cpu.Registers.ReadV(x);
        var flag = (byte)(minuend >= subtrahend ? 1 : 0);
        var result = (byte)(minuend - subtrahend);
        cpu.Registers.WriteV(x, result);
        cpu.Registers.WriteV(0xF, flag);
        if (cpu.VfResultWrittenLast) cpu.Registers.WriteV(x, result);
    }

    public static void ShiftRight(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var value = cpu.Registers.ReadV(x);

        if (cpu.ShiftUsesVy)
        {
            var y = ExtractY(ins);
            value = cpu.Registers.ReadV(y);
        }

        var flag = (byte)(value & 0x1);
        var result = (byte)(value >> 1);
        cpu.Registers.WriteV(x, result);
        cpu.Registers.WriteV(0xF, flag);
        if (cpu.VfResultWrittenLast) cpu.Registers.WriteV(x, result);
    }

    public static void ShiftLeft(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var value = cpu.Registers.ReadV(x);

        if (cpu.ShiftUsesVy)
        {
            var y = ExtractY(ins);
            value = cpu.Registers.ReadV(y);
        }

        var flag = (byte)((value >> 7) & 0x1);
        var result = (byte)(value << 1);
        cpu.Registers.WriteV(x, result);
        cpu.Registers.WriteV(0xF, flag);
        if (cpu.VfResultWrittenLast) cpu.Registers.WriteV(x, result);
    }

    // ---- 0xANNN / 0xBNNN / 0xCXNN ------------------------------------------

    public static void SetIndexRegisterIns(EmulatedCpu cpu, int ins)
    {
        var nnn = ExtractNnn(ins);
        cpu.Registers.WriteI(nnn);
    }

    // Thin dispatcher (still used by tests).
    public static void JumpWithOffsetIns(EmulatedCpu cpu, int ins)
    {
        if (cpu.JumpUsesVx) ExecuteJumpWithVxOffsetIns(cpu, ins);
        else ExecuteJumpWithV0OffsetIns(cpu, ins);
    }

    public static void ExecuteJumpWithV0OffsetIns(EmulatedCpu cpu, int ins)
    {
        var address = ExtractNnn(ins);
        cpu.WriteProgramCounter(address + cpu.Registers.ReadV(0));
    }

    public static void ExecuteJumpWithVxOffsetIns(EmulatedCpu cpu, int ins)
    {
        var address = ExtractNnn(ins);
        var x = ExtractX(ins);
        cpu.WriteProgramCounter(address + cpu.Registers.ReadV(x));
    }

    public static void GenerateRandomNum(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        var randNum = (byte)Random.Shared.Next(0, 256);
        cpu.Registers.WriteV(x, (byte)(randNum & nn));
    }

    // ---- 0xDXYN draw --------------------------------------------------------

    public static void DrawToScreen(EmulatedCpu cpu, int ins)
    {
        var display = cpu.Display;
        var x = cpu.Registers.ReadV(ExtractX(ins)) % display.Width;
        var y = cpu.Registers.ReadV(ExtractY(ins)) % display.Height;
        var n = ExtractN(ins);
        var planeMask = (byte)(display.SelectedPlanes & EmulatedDisplay.AllPlanesMask);

        if (planeMask == 0)
        {
            cpu.Registers.WriteV(0xF, 0);
            if (cpu.DisplayWait) cpu.Bus.Publish<BeginWaitForVBlankEvent>();
            return;
        }

        if (n == 0)
        {
            if (display.IsHighRes)
                SChipRoutines.DrawHighResSprite(cpu, x, y, planeMask);
            else
                DrawLowResSprite(cpu, x, y, 8, planeMask);
        }
        else
        {
            DrawLowResSprite(cpu, x, y, n, planeMask);
        }

        if (cpu.DisplayWait)
        {
            cpu.Bus.Publish<BeginWaitForVBlankEvent>();
        }
    }

    internal static void DrawLowResSprite(EmulatedCpu cpu, int sx, int sy, int height, byte planeMask)
    {
        var display = cpu.Display;
        display.WritePixels(displayPixels =>
        {
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

                    var row = cpu.Memory.Read(cpu.Registers.ReadIWithOffset(spriteBase + y));
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

            cpu.Registers.WriteV(0xF, collision);
        });
    }

    // ---- 0xEX* keyboard skips -----------------------------------------------

    public static void SkipNextInsIfKeyIsPressed(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var key = cpu.Registers.ReadV(x);
        cpu.Bus.Publish(new KeyIsPressedSkipEvent(key));
    }

    public static void SkipNextInsIfKeyIsReleased(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var key = cpu.Registers.ReadV(x);
        cpu.Bus.Publish(new KeyIsReleasedSkipEvent(key));
    }

    // ---- 0xFX** timer / system ops ------------------------------------------

    public static void ReadDelayTimer(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        cpu.Registers.WriteV(x, cpu.Registers.ReadDt());
    }

    public static void WaitForKeyPressAndRelease(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        cpu.Bus.Publish(new BeginWaitForKeyEvent(x));
    }

    public static void SetDelayTimer(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        cpu.Registers.WriteDt(cpu.Registers.ReadV(x));
    }

    public static void SetSoundTimer(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        cpu.Registers.WriteSt(cpu.Registers.ReadV(x));
    }

    public static void AddVxToI(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var i = cpu.Registers.ReadI();
        var vx = cpu.Registers.ReadV(x);
        cpu.Registers.WriteI(i + vx);
    }

    public static void LoadLowResFontCharacter(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var value = cpu.Registers.ReadV(x);
        cpu.Registers.WriteI((value & 0x0F) * Chip8Interpreter.LowRestFontCharWidth + Chip8Interpreter.LowResFontBaseAddress);
    }

    public static void StoreBcdInMemory(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        var bcd = cpu.Registers.ReadV(x);
        cpu.Memory.Write(cpu.Registers.ReadIWithOffset(0), (byte)(bcd / 100));
        cpu.Memory.Write(cpu.Registers.ReadIWithOffset(1), (byte)(bcd / 10 % 10));
        cpu.Memory.Write(cpu.Registers.ReadIWithOffset(2), (byte)(bcd % 10));
    }

    // FX55/FX65 : store/load V0..Vx. Quirk-sensitive (inc I or keep I).
    // Thin dispatchers (still used by tests). Production dispatch picks a variant at flag-set time.

    public static void LoadRegisters(EmulatedCpu cpu, int ins)
    {
        if (cpu.LoadStoreIncrementsI) ExecuteLoadRegistersIncIIns(cpu, ins);
        else ExecuteLoadRegistersKeepIIns(cpu, ins);
    }

    public static void ExecuteLoadRegistersKeepIIns(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        for (var i = 0; i <= x; i++)
        {
            cpu.Registers.WriteV(i, cpu.Memory.Read(cpu.Registers.ReadIWithOffset(i)));
        }
    }

    public static void ExecuteLoadRegistersIncIIns(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        for (var i = 0; i <= x; i++)
        {
            cpu.Registers.WriteV(i, cpu.Memory.Read(cpu.Registers.ReadIWithOffset(i)));
        }
        cpu.Registers.WriteI(cpu.Registers.ReadI() + x + 1);
    }

    public static void StoreRegisters(EmulatedCpu cpu, int ins)
    {
        if (cpu.LoadStoreIncrementsI) ExecuteStoreRegistersIncIIns(cpu, ins);
        else ExecuteStoreRegistersKeepIIns(cpu, ins);
    }

    public static void ExecuteStoreRegistersKeepIIns(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        for (var i = 0; i <= x; i++)
        {
            cpu.Memory.Write(cpu.Registers.ReadIWithOffset(i), cpu.Registers.ReadV(i));
        }
    }

    public static void ExecuteStoreRegistersIncIIns(EmulatedCpu cpu, int ins)
    {
        var x = ExtractX(ins);
        for (var i = 0; i <= x; i++)
        {
            cpu.Memory.Write(cpu.Registers.ReadIWithOffset(i), cpu.Registers.ReadV(i));
        }
        cpu.Registers.WriteI(cpu.Registers.ReadI() + x + 1);
    }
}
