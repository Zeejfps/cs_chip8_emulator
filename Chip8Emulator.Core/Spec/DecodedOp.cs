using System.Runtime.CompilerServices;

namespace Chip8Emulator.Core;

internal readonly struct DecodedOp
{
    public readonly OpKind Kind;
    public readonly int Raw;

    public DecodedOp(OpKind kind, int raw)
    {
        Kind = kind;
        Raw = raw;
    }

    public int X
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        get => Chip8Decoder.ExtractX(Raw);
    }

    public int Y
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        get => Chip8Decoder.ExtractY(Raw);
    }

    public int N
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        get => Chip8Decoder.ExtractN(Raw);
    }

    public byte Nn
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        get => Chip8Decoder.ExtractNn(Raw);
    }

    public int Nnn
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        get => Chip8Decoder.ExtractNnn(Raw);
    }
}
