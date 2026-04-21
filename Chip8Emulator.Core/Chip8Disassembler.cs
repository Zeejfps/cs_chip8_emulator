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

        return opcode switch
        {
            0x0 => DisassembleZeroBase(ins, y, n),
            0x1 => $"JP   0x{nnn:X3}",
            0x2 => $"CALL 0x{nnn:X3}",
            0x3 => $"SE   V{x:X}, 0x{nn:X2}",
            0x4 => $"SNE  V{x:X}, 0x{nn:X2}",
            0x5 => n == 0 ? $"SE   V{x:X}, V{y:X}" : DisassembleUnknown(ins),
            0x6 => $"LD   V{x:X}, 0x{nn:X2}",
            0x7 => $"ADD  V{x:X}, 0x{nn:X2}",
            0x8 => DisassembleArithmetic(ins, x, y, n),
            0x9 => n == 0 ? $"SNE  V{x:X}, V{y:X}" : DisassembleUnknown(ins),
            0xA => $"LD   I, 0x{nnn:X3}",
            0xB => $"JP   V0, 0x{nnn:X3}",
            0xC => $"RND  V{x:X}, 0x{nn:X2}",
            0xD => $"DRW  V{x:X}, V{y:X}, {n}",
            0xE => DisassembleKey(ins, x, nn),
            0xF => DisassembleTimer(ins, x, nn),
            _ => DisassembleUnknown(ins)
        };
    }

    private static string DisassembleZeroBase(int ins, int y, int n)
    {
        return y switch
        {
            0xE when n == 0x0 => "CLS",
            0xE when n == 0xE => "RET",
            0xF when n == 0xF => "HIGH",
            0xF when n == 0xE => "LOW",
            0xF when n == 0xB => "SCR",
            0xF when n == 0xC => "SCL",
            0xC => $"SCD  {n}",
            _ => DisassembleUnknown(ins)
        };
    }

    private static string DisassembleArithmetic(int ins, int x, int y, int n)
    {
        return n switch
        {
            0x0 => $"LD   V{x:X}, V{y:X}",
            0x1 => $"OR   V{x:X}, V{y:X}",
            0x2 => $"AND  V{x:X}, V{y:X}",
            0x3 => $"XOR  V{x:X}, V{y:X}",
            0x4 => $"ADD  V{x:X}, V{y:X}",
            0x5 => $"SUB  V{x:X}, V{y:X}",
            0x6 => $"SHR  V{x:X}",
            0x7 => $"SUBN V{x:X}, V{y:X}",
            0xE => $"SHL  V{x:X}",
            _ => DisassembleUnknown(ins)
        };
    }

    private static string DisassembleKey(int ins, int x, byte nn)
    {
        return nn switch
        {
            0x9E => $"SKP  V{x:X}",
            0xA1 => $"SKNP V{x:X}",
            _ => DisassembleUnknown(ins)
        };
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
            default: return DisassembleUnknown(ins);
        }
    }

    private static string DisassembleUnknown(int ins) => $"DW   0x{ins:X4}";
}
