using System.Runtime.CompilerServices;

namespace Chip8Emulator.Core;

internal static class Chip8Decoder
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

    public static DecodedOp Decode(int ins)
    {
        ins &= 0xFFFF;
        return new DecodedOp(Classify(ins), ins);
    }

    private static OpKind Classify(int ins)
    {
        return ((ins & 0xF000) >> 12) switch
        {
            0x0 => ClassifyZero(ins),
            0x1 => OpKind.Jp,
            0x2 => OpKind.Call,
            0x3 => OpKind.SeVxImm,
            0x4 => OpKind.SneVxImm,
            0x5 => ClassifyFiveOp(ins),
            0x6 => OpKind.LdVxImm,
            0x7 => OpKind.AddVxImm,
            0x8 => ClassifyArithmetic(ins),
            0x9 => ExtractN(ins) == 0 ? OpKind.SneVxVy : OpKind.Unknown,
            0xA => OpKind.LdIImm,
            0xB => OpKind.JpV0,
            0xC => OpKind.Rnd,
            0xD => OpKind.Drw,
            0xE => ClassifyKey(ins),
            0xF => ClassifyTimer(ins),
            _ => OpKind.Unknown
        };
    }

    private static OpKind ClassifyZero(int ins)
    {
        if ((ins & 0xFF00) != 0x0000) return OpKind.Unknown;
        var y = ExtractY(ins);
        var n = ExtractN(ins);
        return y switch
        {
            0xC => OpKind.ScrollDown,
            0xD => OpKind.ScrollUp,
            0xE when n == 0x0 => OpKind.Cls,
            0xE when n == 0xE => OpKind.Ret,
            0xF when n == 0xB => OpKind.ScrollRight,
            0xF when n == 0xC => OpKind.ScrollLeft,
            0xF when n == 0xE => OpKind.DisableHires,
            0xF when n == 0xF => OpKind.EnableHires,
            _ => OpKind.Unknown
        };
    }

    private static OpKind ClassifyFiveOp(int ins)
    {
        return ExtractN(ins) switch
        {
            0x0 => OpKind.SeVxVy,
            0x2 => OpKind.StoreRegisterRange,
            0x3 => OpKind.LoadRegisterRange,
            _ => OpKind.Unknown
        };
    }

    private static OpKind ClassifyArithmetic(int ins)
    {
        return ExtractN(ins) switch
        {
            0x0 => OpKind.LdVxVy,
            0x1 => OpKind.OrVxVy,
            0x2 => OpKind.AndVxVy,
            0x3 => OpKind.XorVxVy,
            0x4 => OpKind.AddVxVy,
            0x5 => OpKind.SubVxVy,
            0x6 => OpKind.ShrVx,
            0x7 => OpKind.SubnVxVy,
            0xE => OpKind.ShlVx,
            _ => OpKind.Unknown
        };
    }

    private static OpKind ClassifyKey(int ins)
    {
        return ExtractNn(ins) switch
        {
            0x9E => OpKind.Skp,
            0xA1 => OpKind.Sknp,
            _ => OpKind.Unknown
        };
    }

    private static OpKind ClassifyTimer(int ins)
    {
        // F000 long-load is encoded as F000 (X must be 0); FN01 select-plane uses any X.
        if (ins == 0xF000) return OpKind.LongLoadI;
        var nn = ExtractNn(ins);
        if (nn == 0x01) return OpKind.SelectPlane;
        if (ins == 0xF002) return OpKind.LoadAudioPattern;
        return nn switch
        {
            0x07 => OpKind.LdVxDt,
            0x0A => OpKind.LdVxK,
            0x15 => OpKind.LdDtVx,
            0x18 => OpKind.LdStVx,
            0x1E => OpKind.AddIVx,
            0x29 => OpKind.LdFVx,
            0x30 => OpKind.LdHfVx,
            0x33 => OpKind.LdBVx,
            0x3A => OpKind.SetPitch,
            0x55 => OpKind.LdIVx,
            0x65 => OpKind.LdVxI,
            0x75 => OpKind.SaveFlags,
            0x85 => OpKind.LoadFlags,
            _ => OpKind.Unknown
        };
    }
}
