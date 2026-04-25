namespace Chip8Emulator.Core.Spec;

// Base CHIP-8 opcode handlers plus the quirk-variant handlers for the ambiguous
// CHIP-8 instructions (OR/AND/XOR VF reset, SHR/SHL Vx-vs-Vy, BNNN jump, FX55/FX65
// inc-I). The quirk variants live here because they are alternate interpretations
// of CHIP-8 ops, not later-CPU additions; the Apply* methods pick between them at
// flag-set time.
internal sealed partial class Chip8Interpreter
{
    // ---- 0x0*** system ops --------------------------------------------------

    internal void ClearDisplay(in DecodedOp op)
    {
        Display.Clear();
    }

    internal void ReturnFromSubroutine(in DecodedOp op)
    {
        var address = Stack.Pop();
        Registers.WritePc(address);
    }

    // ---- 0x1NNN / 0x2NNN : jump / call --------------------------------------

    internal void JumpToAddress(in DecodedOp op)
    {
        Registers.WritePc(op.Nnn);
    }

    internal void CallSubroutine(in DecodedOp op)
    {
        Stack.Push(Registers.ReadPc());
        Registers.WritePc(op.Nnn);
    }

    // ---- 0x3XNN / 0x4XNN / 0x9XY0 : conditional skips ------------------------

    internal void SkipNextInsIfRegisterValueEqualsValue(in DecodedOp op)
    {
        if (Registers.ReadV(op.X) == op.Nn)
        {
            AdvanceProgramCounter();
        }
    }

    internal void SkipNextInsIfRegisterValueNotEqualsValue(in DecodedOp op)
    {
        if (Registers.ReadV(op.X) != op.Nn)
        {
            AdvanceProgramCounter();
        }
    }

    internal void SkipNextInsIfRegisterValueNotEqualsRegisterValue(in DecodedOp op)
    {
        if (Registers.ReadV(op.X) != Registers.ReadV(op.Y))
        {
            AdvanceProgramCounter();
        }
    }

    // ---- 0x5XY0 : SE Vx, Vy --------------------------------------------------

    internal void SkipIfVxEqualsVy(in DecodedOp op)
    {
        if (Registers.ReadV(op.X) == Registers.ReadV(op.Y))
        {
            AdvanceProgramCounter();
        }
    }

    // ---- 0x6XNN / 0x7XNN ----------------------------------------------------

    internal void SetRegisterValue(in DecodedOp op)
    {
        Registers.WriteV(op.X, op.Nn);
    }

    internal void AddValueToRegister(in DecodedOp op)
    {
        Registers.WriteV(op.X, (byte)(Registers.ReadV(op.X) + op.Nn));
    }

    // ---- 0x8XY* arithmetic/logic --------------------------------------------

    internal void SetRegisterValueFromRegister(in DecodedOp op)
    {
        Registers.WriteV(op.X, Registers.ReadV(op.Y));
    }

    // Thin dispatcher (still used by tests).
    internal void BitwiseOrOnRegisters(in DecodedOp op)
    {
        if (LogicResetsVf) ExecuteBitwiseOrResetVfIns(in op);
        else ExecuteBitwiseOrPreserveVfIns(in op);
    }

    internal void ExecuteBitwiseOrPreserveVfIns(in DecodedOp op)
    {
        Registers.WriteV(op.X, (byte)(Registers.ReadV(op.X) | Registers.ReadV(op.Y)));
    }

    internal void ExecuteBitwiseOrResetVfIns(in DecodedOp op)
    {
        Registers.WriteV(op.X, (byte)(Registers.ReadV(op.X) | Registers.ReadV(op.Y)));
        Registers.WriteV(0xF, 0);
    }

    // Thin dispatcher (still used by tests).
    internal void BitwiseAndOnRegisters(in DecodedOp op)
    {
        if (LogicResetsVf) ExecuteBitwiseAndResetVfIns(in op);
        else ExecuteBitwiseAndPreserveVfIns(in op);
    }

    internal void ExecuteBitwiseAndPreserveVfIns(in DecodedOp op)
    {
        Registers.WriteV(op.X, (byte)(Registers.ReadV(op.X) & Registers.ReadV(op.Y)));
    }

    internal void ExecuteBitwiseAndResetVfIns(in DecodedOp op)
    {
        Registers.WriteV(op.X, (byte)(Registers.ReadV(op.X) & Registers.ReadV(op.Y)));
        Registers.WriteV(0xF, 0);
    }

    // Thin dispatcher (still used by tests).
    internal void XorRegisterValueFromRegister(in DecodedOp op)
    {
        if (LogicResetsVf) ExecuteXorResetVfIns(in op);
        else ExecuteXorPreserveVfIns(in op);
    }

    internal void ExecuteXorPreserveVfIns(in DecodedOp op)
    {
        Registers.WriteV(op.X, (byte)(Registers.ReadV(op.X) ^ Registers.ReadV(op.Y)));
    }

    internal void ExecuteXorResetVfIns(in DecodedOp op)
    {
        Registers.WriteV(op.X, (byte)(Registers.ReadV(op.X) ^ Registers.ReadV(op.Y)));
        Registers.WriteV(0xF, 0);
    }

    internal void AddValueToRegisterWithCarry(in DecodedOp op)
    {
        var sum = Registers.ReadV(op.X) + Registers.ReadV(op.Y);
        var carry = (byte)(sum > 0xFF ? 1 : 0);
        var result = (byte)sum;
        Registers.WriteV(op.X, result);
        Registers.WriteV(0xF, carry);
        if (VfResultWrittenLast) Registers.WriteV(op.X, result);
    }

    internal void VxSubVy(in DecodedOp op)
    {
        var minuend = Registers.ReadV(op.X);
        var subtrahend = Registers.ReadV(op.Y);
        var flag = (byte)(minuend >= subtrahend ? 1 : 0);
        var result = (byte)(minuend - subtrahend);
        Registers.WriteV(op.X, result);
        Registers.WriteV(0xF, flag);
        if (VfResultWrittenLast) Registers.WriteV(op.X, result);
    }

    internal void VySubVx(in DecodedOp op)
    {
        // NOTE(Zee): y first
        var minuend = Registers.ReadV(op.Y);
        var subtrahend = Registers.ReadV(op.X);
        var flag = (byte)(minuend >= subtrahend ? 1 : 0);
        var result = (byte)(minuend - subtrahend);
        Registers.WriteV(op.X, result);
        Registers.WriteV(0xF, flag);
        if (VfResultWrittenLast) Registers.WriteV(op.X, result);
    }

    internal void ShiftRight(in DecodedOp op)
    {
        var value = Registers.ReadV(op.X);
        if (ShiftUsesVy) value = Registers.ReadV(op.Y);

        var flag = (byte)(value & 0x1);
        var result = (byte)(value >> 1);
        Registers.WriteV(op.X, result);
        Registers.WriteV(0xF, flag);
        if (VfResultWrittenLast) Registers.WriteV(op.X, result);
    }

    internal void ShiftLeft(in DecodedOp op)
    {
        var value = Registers.ReadV(op.X);
        if (ShiftUsesVy) value = Registers.ReadV(op.Y);

        var flag = (byte)((value >> 7) & 0x1);
        var result = (byte)(value << 1);
        Registers.WriteV(op.X, result);
        Registers.WriteV(0xF, flag);
        if (VfResultWrittenLast) Registers.WriteV(op.X, result);
    }

    // ---- 0xANNN / 0xBNNN / 0xCXNN ------------------------------------------

    internal void SetIndexRegisterIns(in DecodedOp op)
    {
        Registers.WriteI(op.Nnn);
    }

    // Thin dispatcher (still used by tests).
    internal void JumpWithOffsetIns(in DecodedOp op)
    {
        if (JumpUsesVx) ExecuteJumpWithVxOffsetIns(in op);
        else ExecuteJumpWithV0OffsetIns(in op);
    }

    internal void ExecuteJumpWithV0OffsetIns(in DecodedOp op)
    {
        Registers.WritePc(op.Nnn + Registers.ReadV(0));
    }

    internal void ExecuteJumpWithVxOffsetIns(in DecodedOp op)
    {
        Registers.WritePc(op.Nnn + Registers.ReadV(op.X));
    }

    internal void GenerateRandomNum(in DecodedOp op)
    {
        var randNum = (byte)Random.Shared.Next(0, 256);
        Registers.WriteV(op.X, (byte)(randNum & op.Nn));
    }

    // ---- 0xDXYN draw --------------------------------------------------------

    internal void DrawToScreen(in DecodedOp op)
    {
        var x = Registers.ReadV(op.X) % Display.Width;
        var y = Registers.ReadV(op.Y) % Display.Height;
        var n = op.N;
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

    internal void SkipNextInsIfKeyIsPressed(in DecodedOp op)
    {
        var key = Registers.ReadV(op.X);
        if (_input.IsKeyPressed(key)) AdvanceProgramCounter();
    }

    internal void SkipNextInsIfKeyIsReleased(in DecodedOp op)
    {
        var key = Registers.ReadV(op.X);
        if (!_input.IsKeyPressed(key)) AdvanceProgramCounter();
    }

    // ---- 0xFX** timer / system ops ------------------------------------------

    internal void ReadDelayTimer(in DecodedOp op)
    {
        Registers.WriteV(op.X, Registers.ReadDt());
    }

    internal void WaitForKeyPressAndRelease(in DecodedOp op)
    {
        _isWaitingForKey = true;
        _keyRegisterIndex = op.X;
    }

    internal void SetDelayTimer(in DecodedOp op)
    {
        Registers.WriteDt(Registers.ReadV(op.X));
    }

    internal void SetSoundTimer(in DecodedOp op)
    {
        Registers.WriteSt(Registers.ReadV(op.X));
    }

    internal void AddVxToI(in DecodedOp op)
    {
        var i = Registers.ReadI();
        var vx = Registers.ReadV(op.X);
        Registers.WriteI(i + vx);
    }

    internal void LoadLowResFontCharacter(in DecodedOp op)
    {
        var value = Registers.ReadV(op.X);
        Registers.WriteI((value & 0x0F) * LowResFontCharWidth + LowResFontBaseAddress);
    }

    internal void StoreBcdInMemory(in DecodedOp op)
    {
        var bcd = Registers.ReadV(op.X);
        WriteMemory(Registers.ReadIWithOffset(0), (byte)(bcd / 100));
        WriteMemory(Registers.ReadIWithOffset(1), (byte)(bcd / 10 % 10));
        WriteMemory(Registers.ReadIWithOffset(2), (byte)(bcd % 10));
    }

    // FX55/FX65 : store/load V0..Vx. Quirk-sensitive (inc I or keep I).
    // Thin dispatchers (still used by tests). Production dispatch picks a variant at flag-set time.

    internal void LoadRegisters(in DecodedOp op)
    {
        if (LoadStoreIncrementsI) ExecuteLoadRegistersIncIIns(in op);
        else ExecuteLoadRegistersKeepIIns(in op);
    }

    internal void ExecuteLoadRegistersKeepIIns(in DecodedOp op)
    {
        for (var i = 0; i <= op.X; i++)
        {
            Registers.WriteV(i, Memory.Read(Registers.ReadIWithOffset(i)));
        }
    }

    internal void ExecuteLoadRegistersIncIIns(in DecodedOp op)
    {
        for (var i = 0; i <= op.X; i++)
        {
            Registers.WriteV(i, Memory.Read(Registers.ReadIWithOffset(i)));
        }
        Registers.WriteI(Registers.ReadI() + op.X + 1);
    }

    internal void StoreRegisters(in DecodedOp op)
    {
        if (LoadStoreIncrementsI) ExecuteStoreRegistersIncIIns(in op);
        else ExecuteStoreRegistersKeepIIns(in op);
    }

    internal void ExecuteStoreRegistersKeepIIns(in DecodedOp op)
    {
        for (var i = 0; i <= op.X; i++)
        {
            WriteMemory(Registers.ReadIWithOffset(i), Registers.ReadV(i));
        }
    }

    internal void ExecuteStoreRegistersIncIIns(in DecodedOp op)
    {
        for (var i = 0; i <= op.X; i++)
        {
            WriteMemory(Registers.ReadIWithOffset(i), Registers.ReadV(i));
        }
        Registers.WriteI(Registers.ReadI() + op.X + 1);
    }
}
