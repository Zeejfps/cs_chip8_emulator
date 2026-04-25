using static Chip8Emulator.Core.Chip8Disassembler;

namespace Chip8Emulator.Core.Spec;

// Base CHIP-8 opcode handlers plus the quirk-variant handlers for the ambiguous
// CHIP-8 instructions (OR/AND/XOR VF reset, SHR/SHL Vx-vs-Vy, BNNN jump, FX55/FX65
// inc-I). The quirk variants live here because they are alternate interpretations
// of CHIP-8 ops, not later-CPU additions; the Apply* methods pick between them at
// flag-set time.
internal sealed partial class Chip8Interpreter
{
    // ---- 0x0*** system ops --------------------------------------------------

    internal void ClearDisplay(int ins)
    {
        Display.Clear();
    }

    internal void ReturnFromSubroutine(int ins)
    {
        var address = Stack.Pop();
        Registers.WritePc(address);
    }

    // ---- 0x1NNN / 0x2NNN : jump / call --------------------------------------

    internal void JumpToAddress(int ins)
    {
        var address = ExtractNnn(ins);
        Registers.WritePc(address);
    }

    internal void CallSubroutine(int ins)
    {
        var address = ExtractNnn(ins);
        Stack.Push(Registers.ReadPc());
        Registers.WritePc(address);
    }

    // ---- 0x3XNN / 0x4XNN / 0x9XY0 : conditional skips ------------------------

    internal void SkipNextInsIfRegisterValueEqualsValue(int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        if (Registers.ReadV(x) == nn)
        {
            AdvanceProgramCounter();
        }
    }

    internal void SkipNextInsIfRegisterValueNotEqualsValue(int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        if (Registers.ReadV(x) != nn)
        {
            AdvanceProgramCounter();
        }
    }

    internal void SkipNextInsIfRegisterValueNotEqualsRegisterValue(int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        if (Registers.ReadV(x) != Registers.ReadV(y))
        {
            AdvanceProgramCounter();
        }
    }

    // ---- 0x5XY0 : SE Vx, Vy (called from FiveOpTable slot 0) ----------------

    internal void SkipIfVxEqualsVy(int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        if (Registers.ReadV(x) == Registers.ReadV(y))
        {
            AdvanceProgramCounter();
        }
    }

    // ---- 0x6XNN / 0x7XNN ----------------------------------------------------

    internal void SetRegisterValue(int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        Registers.WriteV(x, nn);
    }

    internal void AddValueToRegister(int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        Registers.WriteV(x, (byte)(Registers.ReadV(x) + nn));
    }

    // ---- 0x8XY* arithmetic/logic --------------------------------------------

    internal void SetRegisterValueFromRegister(int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        Registers.WriteV(x, Registers.ReadV(y));
    }

    // Thin dispatcher (still used by tests).
    internal void BitwiseOrOnRegisters(int ins)
    {
        if (LogicResetsVf) ExecuteBitwiseOrResetVfIns(ins);
        else ExecuteBitwiseOrPreserveVfIns(ins);
    }

    internal void ExecuteBitwiseOrPreserveVfIns(int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        Registers.WriteV(x, (byte)(Registers.ReadV(x) | Registers.ReadV(y)));
    }

    internal void ExecuteBitwiseOrResetVfIns(int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        Registers.WriteV(x, (byte)(Registers.ReadV(x) | Registers.ReadV(y)));
        Registers.WriteV(0xF, 0);
    }

    // Thin dispatcher (still used by tests).
    internal void BitwiseAndOnRegisters(int ins)
    {
        if (LogicResetsVf) ExecuteBitwiseAndResetVfIns(ins);
        else ExecuteBitwiseAndPreserveVfIns(ins);
    }

    internal void ExecuteBitwiseAndPreserveVfIns(int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        Registers.WriteV(x, (byte)(Registers.ReadV(x) & Registers.ReadV(y)));
    }

    internal void ExecuteBitwiseAndResetVfIns(int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        Registers.WriteV(x, (byte)(Registers.ReadV(x) & Registers.ReadV(y)));
        Registers.WriteV(0xF, 0);
    }

    // Thin dispatcher (still used by tests).
    internal void XorRegisterValueFromRegister(int ins)
    {
        if (LogicResetsVf) ExecuteXorResetVfIns(ins);
        else ExecuteXorPreserveVfIns(ins);
    }

    internal void ExecuteXorPreserveVfIns(int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        Registers.WriteV(x, (byte)(Registers.ReadV(x) ^ Registers.ReadV(y)));
    }

    internal void ExecuteXorResetVfIns(int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        Registers.WriteV(x, (byte)(Registers.ReadV(x) ^ Registers.ReadV(y)));
        Registers.WriteV(0xF, 0);
    }

    internal void AddValueToRegisterWithCarry(int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        var sum = Registers.ReadV(x) + Registers.ReadV(y);
        var carry = (byte)(sum > 0xFF ? 1 : 0);
        var result = (byte)sum;
        Registers.WriteV(x, result);
        Registers.WriteV(0xF, carry);
        if (VfResultWrittenLast) Registers.WriteV(x, result);
    }

    internal void VxSubVy(int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        var minuend = Registers.ReadV(x);
        var subtrahend = Registers.ReadV(y);
        var flag = (byte)(minuend >= subtrahend ? 1 : 0);
        var result = (byte)(minuend - subtrahend);
        Registers.WriteV(x, result);
        Registers.WriteV(0xF, flag);
        if (VfResultWrittenLast) Registers.WriteV(x, result);
    }

    internal void VySubVx(int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        // NOTE(Zee): y first
        var minuend = Registers.ReadV(y);
        var subtrahend = Registers.ReadV(x);
        var flag = (byte)(minuend >= subtrahend ? 1 : 0);
        var result = (byte)(minuend - subtrahend);
        Registers.WriteV(x, result);
        Registers.WriteV(0xF, flag);
        if (VfResultWrittenLast) Registers.WriteV(x, result);
    }

    internal void ShiftRight(int ins)
    {
        var x = ExtractX(ins);
        var value = Registers.ReadV(x);

        if (ShiftUsesVy)
        {
            var y = ExtractY(ins);
            value = Registers.ReadV(y);
        }

        var flag = (byte)(value & 0x1);
        var result = (byte)(value >> 1);
        Registers.WriteV(x, result);
        Registers.WriteV(0xF, flag);
        if (VfResultWrittenLast) Registers.WriteV(x, result);
    }

    internal void ShiftLeft(int ins)
    {
        var x = ExtractX(ins);
        var value = Registers.ReadV(x);

        if (ShiftUsesVy)
        {
            var y = ExtractY(ins);
            value = Registers.ReadV(y);
        }

        var flag = (byte)((value >> 7) & 0x1);
        var result = (byte)(value << 1);
        Registers.WriteV(x, result);
        Registers.WriteV(0xF, flag);
        if (VfResultWrittenLast) Registers.WriteV(x, result);
    }

    // ---- 0xANNN / 0xBNNN / 0xCXNN ------------------------------------------

    internal void SetIndexRegisterIns(int ins)
    {
        var nnn = ExtractNnn(ins);
        Registers.WriteI(nnn);
    }

    // Thin dispatcher (still used by tests).
    internal void JumpWithOffsetIns(int ins)
    {
        if (JumpUsesVx) ExecuteJumpWithVxOffsetIns(ins);
        else ExecuteJumpWithV0OffsetIns(ins);
    }

    internal void ExecuteJumpWithV0OffsetIns(int ins)
    {
        var address = ExtractNnn(ins);
        Registers.WritePc(address + Registers.ReadV(0));
    }

    internal void ExecuteJumpWithVxOffsetIns(int ins)
    {
        var address = ExtractNnn(ins);
        var x = ExtractX(ins);
        Registers.WritePc(address + Registers.ReadV(x));
    }

    internal void GenerateRandomNum(int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        var randNum = (byte)Random.Shared.Next(0, 256);
        Registers.WriteV(x, (byte)(randNum & nn));
    }

    // ---- 0xDXYN draw --------------------------------------------------------

    internal void DrawToScreen(int ins)
    {
        var x = Registers.ReadV(ExtractX(ins)) % Display.Width;
        var y = Registers.ReadV(ExtractY(ins)) % Display.Height;
        var n = ExtractN(ins);
        var planeMask = (byte)(Display.SelectedPlanes & Chip8Display.AllPlanesMask);

        if (planeMask == 0)
        {
            Registers.WriteV(0xF, 0);
            if (DisplayWait) _waitForVBlank = true;
            return;
        }

        if (n == 0)
        {
            if (Display.IsHighRes)
                DrawHighResSprite(x, y, planeMask);
            else
                DrawLowResSprite(x, y, 8, planeMask);
        }
        else
        {
            DrawLowResSprite(x, y, n, planeMask);
        }

        if (DisplayWait)
        {
            _waitForVBlank = true;
        }
    }

    private void DrawLowResSprite(int sx, int sy, int height, byte planeMask)
    {
        Display.WritePixels(displayPixels =>
        {
            var width = Display.Width;
            var displayHeight = Display.Height;
            var wrap = SpritesWrap;
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

                    var row = Memory.Read(Registers.ReadIWithOffset(spriteBase + y));
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

            Registers.WriteV(0xF, collision);
        });
    }

    // ---- 0xEX* keyboard skips -----------------------------------------------

    internal void SkipNextInsIfKeyIsPressed(int ins)
    {
        var x = ExtractX(ins);
        var key = Registers.ReadV(x);
        if (_input.IsKeyPressed(key)) AdvanceProgramCounter();
    }

    internal void SkipNextInsIfKeyIsReleased(int ins)
    {
        var x = ExtractX(ins);
        var key = Registers.ReadV(x);
        if (!_input.IsKeyPressed(key)) AdvanceProgramCounter();
    }

    // ---- 0xFX** timer / system ops ------------------------------------------

    internal void ReadDelayTimer(int ins)
    {
        var x = ExtractX(ins);
        Registers.WriteV(x, Registers.ReadDt());
    }

    internal void WaitForKeyPressAndRelease(int ins)
    {
        var x = ExtractX(ins);
        _isWaitingForKey = true;
        _keyRegisterIndex = x;
    }

    internal void SetDelayTimer(int ins)
    {
        var x = ExtractX(ins);
        Registers.WriteDt(Registers.ReadV(x));
    }

    internal void SetSoundTimer(int ins)
    {
        var x = ExtractX(ins);
        Registers.WriteSt(Registers.ReadV(x));
    }

    internal void AddVxToI(int ins)
    {
        var x = ExtractX(ins);
        var i = Registers.ReadI();
        var vx = Registers.ReadV(x);
        Registers.WriteI(i + vx);
    }

    internal void LoadLowResFontCharacter(int ins)
    {
        var x = ExtractX(ins);
        var value = Registers.ReadV(x);
        Registers.WriteI((value & 0x0F) * LowResFontCharWidth + LowResFontBaseAddress);
    }

    internal void StoreBcdInMemory(int ins)
    {
        var x = ExtractX(ins);
        var bcd = Registers.ReadV(x);
        Memory.Write(Registers.ReadIWithOffset(0), (byte)(bcd / 100));
        Memory.Write(Registers.ReadIWithOffset(1), (byte)(bcd / 10 % 10));
        Memory.Write(Registers.ReadIWithOffset(2), (byte)(bcd % 10));
    }

    // FX55/FX65 : store/load V0..Vx. Quirk-sensitive (inc I or keep I).
    // Thin dispatchers (still used by tests). Production dispatch picks a variant at flag-set time.

    internal void LoadRegisters(int ins)
    {
        if (LoadStoreIncrementsI) ExecuteLoadRegistersIncIIns(ins);
        else ExecuteLoadRegistersKeepIIns(ins);
    }

    internal void ExecuteLoadRegistersKeepIIns(int ins)
    {
        var x = ExtractX(ins);
        for (var i = 0; i <= x; i++)
        {
            Registers.WriteV(i, Memory.Read(Registers.ReadIWithOffset(i)));
        }
    }

    internal void ExecuteLoadRegistersIncIIns(int ins)
    {
        var x = ExtractX(ins);
        for (var i = 0; i <= x; i++)
        {
            Registers.WriteV(i, Memory.Read(Registers.ReadIWithOffset(i)));
        }
        Registers.WriteI(Registers.ReadI() + x + 1);
    }

    internal void StoreRegisters(int ins)
    {
        if (LoadStoreIncrementsI) ExecuteStoreRegistersIncIIns(ins);
        else ExecuteStoreRegistersKeepIIns(ins);
    }

    internal void ExecuteStoreRegistersKeepIIns(int ins)
    {
        var x = ExtractX(ins);
        for (var i = 0; i <= x; i++)
        {
            Memory.Write(Registers.ReadIWithOffset(i), Registers.ReadV(i));
        }
    }

    internal void ExecuteStoreRegistersIncIIns(int ins)
    {
        var x = ExtractX(ins);
        for (var i = 0; i <= x; i++)
        {
            Memory.Write(Registers.ReadIWithOffset(i), Registers.ReadV(i));
        }
        Registers.WriteI(Registers.ReadI() + x + 1);
    }
}
