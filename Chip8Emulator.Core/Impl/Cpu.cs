using System.Runtime.CompilerServices;
using static Chip8Emulator.Core.Chip8Disassembler;

namespace Chip8Emulator.Core.Impl;

internal static class Cpu
{
    private static readonly Action<Chip8Machine, int>[] OpcodeTable =
    [
        ExecuteZeroBaseIns,                                      // 0
        ExecuteJumpToAddressIns,                                 // 1
        ExecuteCallSubroutineIns,                                // 2
        ExecuteSkipNextInsIfRegisterValueEqualsValueIns,         // 3
        ExecuteSkipNextInsIfRegisterValueNotEqualsValueIns,      // 4
        ExecuteSkipNextInsIfRegisterValueEqualsRegisterValue,    // 5
        ExecuteSetRegisterValueIns,                              // 6
        ExecuteAddValueToRegisterIns,                            // 7
        ExecuteArithmeticOperationIns,                           // 8
        ExecuteSkipNextInsIfRegisterValueNotEqualsRegisterValue, // 9
        ExecuteSetIndexRegisterIns,                              // A
        ExecuteJumpWithOffsetIns,                                // B
        ExecuteGenerateRandomNumIns,                             // C
        ExeuteDrawToScreenIns,                                   // D
        ExecuteSkipNextInsIfKeyIsPressedOrReleased,              // E
        ExecuteTimerIns,                                         // F
    ];

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void FetchDecodeExecute(Chip8Machine machine)
    {
        var ins = Fetch(machine);
        var opcode = (ins & 0xF000) >> 12;
        var execute = OpcodeTable[opcode];
        execute(machine, ins);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Fetch(Chip8Machine machine)
    {
        var pc = machine.ReadProgramCounter();
        var ins = machine.ReadMemory(pc) << 8 | machine.ReadMemory(pc + 1);
        machine.AdvanceProgramCounter();
        return ins;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteZeroBaseIns(Chip8Machine machine, int ins)
    {
        if ((ins & 0xFF00) != 0x0000) return;

        var lo = ins & 0x00FF;

        switch (lo)
        {
            case 0xE0: ExecuteClearDisplayIns(machine);           return;
            case 0xEE: ExecuteReturnFromSubroutineIns(machine);   return;
            case 0xFF: ExecuteEnableHiresModeIns(machine);        return;
            case 0xFE: ExecuteDisableHiresModeIns(machine);       return;
            case 0xFB: machine.ScrollDisplayRight(4);             return;
            case 0xFC: machine.ScrollDisplayLeft(4);              return;
        }

        // 00CN — S-CHIP: scroll display down N rows.
        if ((lo & 0xF0) == 0xC0)
        {
            machine.ScrollDisplayDown(lo & 0x0F);
            return;
        }

        // 00DN — XO-CHIP: scroll display up N rows.
        if ((lo & 0xF0) == 0xD0)
        {
            machine.ScrollDisplayUp(lo & 0x0F);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void ExecuteEnableHiresModeIns(Chip8Machine machine)
    {
        machine.EnableHighResMode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void ExecuteDisableHiresModeIns(Chip8Machine machine)
    {
        machine.DisableHighResMode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteTimerIns(Chip8Machine machine, int ins)
    {
        var op = ins & 0x00FF;
        switch (op)
        {
            case 0x00:
                // F000 NNNN — XO-CHIP long load I with the 16-bit word following the opcode.
                if (ExtractX(ins) == 0) ExecuteLongLoadIndexRegister(machine);
                break;
            case 0x07:
                ExecuteReadDelayTimer(machine, ins);
                break;
            case 0x0A:
                ExecuteWaitForKeyPress(machine, ins);
                break;
            case 0x15:
                ExecuteSetDelayTimer(machine, ins);
                break;
            case 0x18:
                ExecuteSetSoundTimer(machine, ins);
                break;
            case 0x1E:
                ExecuteAddVxToI(machine, ins);
                break;
            case 0x29:
                ExecuteLoadLowResFontCharacter(machine, ins);
                break;
            case 0x30:
                ExecuteLoadHighResFontCharacter(machine, ins);
                break;
            case 0x33:
                ExecuteStoreBcdInMemory(machine, ins);
                break;
            case 0x55:
                ExecuteStoreRegisters(machine, ins);
                break;
            case 0x65:
                ExecuteLoadRegisters(machine, ins);
                break;
            // Unknown FXnn: no-op so extended-variant ROMs don't halt.
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteLongLoadIndexRegister(Chip8Machine machine)
    {
        var pc = machine.ReadProgramCounter();
        var hi = machine.ReadMemory(pc);
        var lo = machine.ReadMemory(pc + 1);
        machine.WriteIndexRegister((hi << 8) | lo);
        machine.AdvanceProgramCounter();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteStoreBcdInMemory(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var bcd = machine.ReadGeneralPurposeRegister(x);
        machine.WriteMemory(machine.ReadIndexRegisterWithOffset(0), (byte)(bcd / 100));
        machine.WriteMemory(machine.ReadIndexRegisterWithOffset(1), (byte)(bcd / 10 % 10));
        machine.WriteMemory(machine.ReadIndexRegisterWithOffset(2), (byte)(bcd % 10));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteLoadRegisters(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        for (var i = 0; i <= x; i++)
        {
            machine.WriteGeneralPurposeRegister(i, machine.ReadMemory(machine.ReadIndexRegisterWithOffset(i)));
        }

        if (machine.LoadStoreIncrementsI)
        {
            machine.WriteIndexRegister(machine.ReadIndexRegister() + x + 1);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteStoreRegisters(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        for (var i = 0; i <= x; i++)
        {
            machine.WriteMemory(machine.ReadIndexRegisterWithOffset(i), machine.ReadGeneralPurposeRegister(i));
        }

        if (machine.LoadStoreIncrementsI)
        {
            machine.WriteIndexRegister(machine.ReadIndexRegister() + x + 1);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteLoadLowResFontCharacter(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var value = machine.ReadGeneralPurposeRegister(x);
        machine.WriteIndexRegister((value & 0x0F) * Chip8Machine.LowRestFontCharWidth + Chip8Machine.LowResFontBaseAddress);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteLoadHighResFontCharacter(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var value = machine.ReadGeneralPurposeRegister(x);
        machine.WriteIndexRegister((value & 0x0F) * Chip8Machine.HighRestFontCharWidth + Chip8Machine.HighResFontBaseAddress);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteAddVxToI(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var i = machine.ReadIndexRegister();
        var vx = machine.ReadGeneralPurposeRegister(x);
        machine.WriteIndexRegister(i + vx);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteWaitForKeyPress(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        machine.BeginWaitForKey(x);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteSetSoundTimer(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        machine.WriteSoundTimer(machine.ReadGeneralPurposeRegister(x));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteSetDelayTimer(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        machine.WriteDelayTimer(machine.ReadGeneralPurposeRegister(x));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteReadDelayTimer(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        machine.WriteGeneralPurposeRegister(x, machine.ReadDelayTimer());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteSkipNextInsIfKeyIsPressedOrReleased(Chip8Machine machine, int ins)
    {
        var op = ins & 0x00FF;
        if (op == 0x9E)
        {
            ExecuteSkipNextInsIfKeyIsPressed(machine, ins);
        }
        else if (op == 0xA1)
        {
            ExecuteSkipNextInsIfKeyIsReleased(machine, ins);
        }
        // Unknown EXnn: no-op.
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteSkipNextInsIfKeyIsPressed(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var key = machine.ReadGeneralPurposeRegister(x);
        if (machine.Input.IsKeyPressed(key))
        {
            machine.AdvanceProgramCounter();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteSkipNextInsIfKeyIsReleased(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var key = machine.ReadGeneralPurposeRegister(x);
        if (!machine.Input.IsKeyPressed(key))
        {
            machine.AdvanceProgramCounter();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteGenerateRandomNumIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        var randNum = (byte)Random.Shared.Next(0, 256);
        machine.WriteGeneralPurposeRegister(x, (byte)(randNum & nn));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteSkipNextInsIfRegisterValueNotEqualsRegisterValue(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        if (machine.ReadGeneralPurposeRegister(x) != machine.ReadGeneralPurposeRegister(y))
        {
            machine.AdvanceProgramCounter();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteSkipNextInsIfRegisterValueEqualsValueIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        if (machine.ReadGeneralPurposeRegister(x) == nn)
        {
            machine.AdvanceProgramCounter();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteSkipNextInsIfRegisterValueNotEqualsValueIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        if (machine.ReadGeneralPurposeRegister(x) != nn)
        {
            machine.AdvanceProgramCounter();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteSkipNextInsIfRegisterValueEqualsRegisterValue(Chip8Machine machine, int ins)
    {
        var n = ExtractN(ins);
        switch (n)
        {
            case 0:
                var x = ExtractX(ins);
                var y = ExtractY(ins);
                if (machine.ReadGeneralPurposeRegister(x) == machine.ReadGeneralPurposeRegister(y))
                {
                    machine.AdvanceProgramCounter();
                }
                break;
            case 2:
                ExecuteStoreRegisterRange(machine, ins);
                break;
            case 3:
                ExecuteLoadRegisterRange(machine, ins);
                break;
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

    private static void ExecuteStoreRegisterRange(Chip8Machine machine, int ins)  // 5XY2
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

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteCallSubroutineIns(Chip8Machine machine, int ins)
    {
        var address = ExtractNnn(ins);
        machine.PushStack(machine.ReadProgramCounter());
        machine.WriteProgramCounter(address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExeuteDrawToScreenIns(Chip8Machine machine, int ins)
    {
        var display = machine.Display;
        var x = machine.ReadGeneralPurposeRegister(ExtractX(ins)) % display.Width;
        var y = machine.ReadGeneralPurposeRegister(ExtractY(ins)) % display.Height;
        var n = ExtractN(ins);

        if (n == 0)
        {
            if (display.IsHighRes)
                DrawHighResSprite(machine, x, y);
            else
                DrawLowResSprite(machine, x, y, 8);
        }
        else
        {
            DrawLowResSprite(machine, x, y, n);
        }

        if (machine.DisplayWait)
        {
            machine.BeginWaitForVBlank();
        }
    }

    private static void DrawHighResSprite(Chip8Machine machine, int x, int y)
    {
        // S-CHIP 1.1 DXY0 hi-res collision semantics:
        // VF = number of sprite rows with at least one collision
        //    + number of sprite rows clipped off the bottom edge (when not wrapping).
        var display = machine.Display;
        var displayPixels = display.Pixels.Span;
        var width = display.Width;
        var height = display.Height;
        var wrap = machine.SpritesWrap;
        var collidedRows = 0;
        var clippedRows = 0;
        for (var i = 0; i < 16; i++)
        {
            var dstY = y + i;
            if (wrap)
            {
                dstY %= height;
            }
            else if (dstY >= height)
            {
                clippedRows = 16 - i;
                break;
            }

            var offset = i * 2;
            var spritePixelsRow = (ushort)(machine.ReadMemory(machine.ReadIndexRegisterWithOffset(offset)) << 8 |
                                           machine.ReadMemory(machine.ReadIndexRegisterWithOffset(offset + 1)));
            var rowCollided = false;
            for (var bit = 0; bit < 16; bit++)
            {
                var dstX = x + bit;
                if (wrap) dstX %= width;
                else if (dstX >= width) break;

                var spritePixel = (byte)((spritePixelsRow >> (15 - bit)) & 1);
                var dstIndex = dstY * width + dstX;
                var before = displayPixels[dstIndex];
                if ((before & spritePixel) != 0) rowCollided = true;
                displayPixels[dstIndex] = (byte)(before ^ spritePixel);
            }
            if (rowCollided) collidedRows++;
        }

        machine.WriteGeneralPurposeRegister(0xF, (byte)(collidedRows + clippedRows));
    }

    private static void DrawLowResSprite(Chip8Machine machine, int sx, int sy, int height)
    {
        var display = machine.Display;
        var displayPixels = display.Pixels.Span;
        var width = display.Width;
        var displayHeight = display.Height;
        var wrap = machine.SpritesWrap;
        byte collision = 0;
        for (var y = 0; y < height; y++)
        {
            var dstY = sy + y;
            if (wrap) dstY %= displayHeight;
            else if (dstY >= displayHeight) break;

            var spritePixelsRow = machine.ReadMemory(machine.ReadIndexRegisterWithOffset(y));
            for (var bit = 0; bit < 8; bit++)
            {
                var dstX = sx + bit;
                if (wrap) dstX %= width;
                else if (dstX >= width) break;

                var spritePixel = (byte)((spritePixelsRow >> (7 - bit)) & 1);
                var dstIndex = dstY * width + dstX;
                var before = displayPixels[dstIndex];
                collision |= (byte)(before & spritePixel);
                displayPixels[dstIndex] = (byte)(before ^ spritePixel);
            }
        }

        machine.WriteGeneralPurposeRegister(0xF, collision != 0 ? (byte)1 : (byte)0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteSetRegisterValueIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        machine.WriteGeneralPurposeRegister(x, nn);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteArithmeticOperationIns(Chip8Machine machine, int ins)
    {
        var lo = ins & 0x000F;
        switch (lo)
        {
            case 0:
                ExecuteSetRegisterValueFromRegisterIns(machine, ins);
                break;
            case 1:
                ExecuteBitwiseOrOnRegistersIns(machine, ins);
                break;
            case 2:
                ExecuteBitwiseAndOnRegistersIns(machine, ins);
                break;
            case 3:
                ExecuteXorRegisterValueFromRegisterIns(machine, ins);
                break;
            case 4:
                ExecuteAddValueToRegisterWithCarryIns(machine, ins);
                break;
            case 5:
                ExecuteVxSubVyIns(machine, ins);
                break;
            case 6:
                ExecuteShiftRightIns(machine, ins);
                break;
            case 7:
                ExecuteVySubVxIns(machine, ins);
                break;
            case 0xE:
                ExecuteShiftLeftIns(machine, ins);
                break;
            // Unknown 8XYn: no-op.
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteBitwiseOrOnRegistersIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        machine.WriteGeneralPurposeRegister(x, (byte)(machine.ReadGeneralPurposeRegister(x) | machine.ReadGeneralPurposeRegister(y)));

        if (machine.LogicResetsVf)
        {
            machine.WriteGeneralPurposeRegister(0xF, 0);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteBitwiseAndOnRegistersIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        machine.WriteGeneralPurposeRegister(x, (byte)(machine.ReadGeneralPurposeRegister(x) & machine.ReadGeneralPurposeRegister(y)));

        if (machine.LogicResetsVf)
        {
            machine.WriteGeneralPurposeRegister(0xF, 0);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteSetRegisterValueFromRegisterIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        machine.WriteGeneralPurposeRegister(x, machine.ReadGeneralPurposeRegister(y));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteXorRegisterValueFromRegisterIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        machine.WriteGeneralPurposeRegister(x, (byte)(machine.ReadGeneralPurposeRegister(x) ^ machine.ReadGeneralPurposeRegister(y)));

        if (machine.LogicResetsVf)
        {
            machine.WriteGeneralPurposeRegister(0xF, 0);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteAddValueToRegisterIns(Chip8Machine machine, int ins)
    {
        var x = ExtractX(ins);
        var nn = ExtractNn(ins);
        machine.WriteGeneralPurposeRegister(x, (byte)(machine.ReadGeneralPurposeRegister(x) + nn));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteSetIndexRegisterIns(Chip8Machine machine, int ins)
    {
        var nnn = ExtractNnn(ins);
        machine.WriteIndexRegister(nnn);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteJumpToAddressIns(Chip8Machine machine, int ins)
    {
        var address = ExtractNnn(ins);
        machine.WriteProgramCounter(address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteJumpWithOffsetIns(Chip8Machine machine, int ins)
    {
        var address = ExtractNnn(ins);
        if (machine.JumpUsesVx)
        {
            var x = ExtractX(ins);
            machine.WriteProgramCounter(address + machine.ReadGeneralPurposeRegister(x));
        }
        else
        {
            machine.WriteProgramCounter(address + machine.ReadGeneralPurposeRegister(0));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteReturnFromSubroutineIns(Chip8Machine machine)
    {
        var address = machine.PopStack();
        machine.WriteProgramCounter(address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ExecuteClearDisplayIns(Chip8Machine machine)
    {
        machine.ClearDisplay();
    }
}
