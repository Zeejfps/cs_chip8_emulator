using System.Runtime.CompilerServices;

namespace Chip8Emulator.Core;

public static class Chip8Disassembler
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int ExtractNnn(int ins) => ins & 0x0FFF;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static byte ExtractNn(int ins) => (byte)(ins & 0x00FF);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int ExtractN(int ins) => ins & 0x000F;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int ExtractX(int ins) => (ins & 0x0F00) >> 8;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int ExtractY(int ins) => (ins & 0x00F0) >> 4;

    public static string Disassemble(int ins)
    {
        ins &= 0xFFFF;
        var opcode = (ins & 0xF000) >> 12;
        var x = ExtractX(ins);
        var y = ExtractY(ins);
        var n = ExtractN(ins);
        var nn = ExtractNn(ins);
        var nnn = ExtractNnn(ins);

        switch (opcode)
        {
            case 0x0:
                return DisassembleZeroBase(ins, y, n);
            case 0x1:
                return $"JP   0x{nnn:X3}";
            case 0x2:
                return $"CALL 0x{nnn:X3}";
            case 0x3:
                return $"SE   V{x:X}, 0x{nn:X2}";
            case 0x4:
                return $"SNE  V{x:X}, 0x{nn:X2}";
            case 0x5:
                return n == 0 ? $"SE   V{x:X}, V{y:X}" : Unknown(ins);
            case 0x6:
                return $"LD   V{x:X}, 0x{nn:X2}";
            case 0x7:
                return $"ADD  V{x:X}, 0x{nn:X2}";
            case 0x8:
                return DisassembleArithmetic(ins, x, y, n);
            case 0x9:
                return n == 0 ? $"SNE  V{x:X}, V{y:X}" : Unknown(ins);
            case 0xA:
                return $"LD   I, 0x{nnn:X3}";
            case 0xB:
                return $"JP   V0, 0x{nnn:X3}";
            case 0xC:
                return $"RND  V{x:X}, 0x{nn:X2}";
            case 0xD:
                return $"DRW  V{x:X}, V{y:X}, {n}";
            case 0xE:
                return DisassembleKey(ins, x, nn);
            case 0xF:
                return DisassembleTimer(ins, x, nn);
            default:
                return Unknown(ins);
        }
    }

    private static string DisassembleZeroBase(int ins, int y, int n)
    {
        if (y == 0xE && n == 0x0) return "CLS";
        if (y == 0xE && n == 0xE) return "RET";
        if (y == 0xF && n == 0xF) return "HIGH";
        if (y == 0xF && n == 0xE) return "LOW";
        if (y == 0xF && n == 0xB) return "SCR";
        if (y == 0xF && n == 0xC) return "SCL";
        if (y == 0xC) return $"SCD  {n}";
        return Unknown(ins);
    }

    private static string DisassembleArithmetic(int ins, int x, int y, int n)
    {
        switch (n)
        {
            case 0x0: return $"LD   V{x:X}, V{y:X}";
            case 0x1: return $"OR   V{x:X}, V{y:X}";
            case 0x2: return $"AND  V{x:X}, V{y:X}";
            case 0x3: return $"XOR  V{x:X}, V{y:X}";
            case 0x4: return $"ADD  V{x:X}, V{y:X}";
            case 0x5: return $"SUB  V{x:X}, V{y:X}";
            case 0x6: return $"SHR  V{x:X}";
            case 0x7: return $"SUBN V{x:X}, V{y:X}";
            case 0xE: return $"SHL  V{x:X}";
            default: return Unknown(ins);
        }
    }

    private static string DisassembleKey(int ins, int x, byte nn)
    {
        if (nn == 0x9E) return $"SKP  V{x:X}";
        if (nn == 0xA1) return $"SKNP V{x:X}";
        return Unknown(ins);
    }

    private static string DisassembleTimer(int ins, int x, byte nn)
    {
        switch (nn)
        {
            case 0x07: return $"LD   V{x:X}, DT";
            case 0x0A: return $"LD   V{x:X}, K";
            case 0x15: return $"LD   DT, V{x:X}";
            case 0x18: return $"LD   ST, V{x:X}";
            case 0x1E: return $"ADD  I, V{x:X}";
            case 0x29: return $"LD   F, V{x:X}";
            case 0x30: return $"LD   HF, V{x:X}";
            case 0x33: return $"LD   B, V{x:X}";
            case 0x55: return $"LD   [I], V{x:X}";
            case 0x65: return $"LD   V{x:X}, [I]";
            default: return Unknown(ins);
        }
    }

    private static string Unknown(int ins) => $"DW   0x{ins:X4}";
}
