namespace Chip8Emulator.Core.Spec;

// XO-Chip additions: scroll up N, 16-bit I register (long load), bitplane
// selection, audio pattern buffer + pitch, register range load/store.
internal sealed partial class Chip8Interpreter
{
    // ---- 00DN : scroll display up N rows ------------------------------------

    internal void ScrollDisplayUp(in DecodedOp op)
    {
        Display.ScrollUp(op.N);
    }

    // ---- F000 NNNN : long load I --------------------------------------------

    internal void LongLoadIndexRegister(in DecodedOp op)
    {
        var pc = Registers.ReadPc();
        var hi = Memory.Read(pc);
        var lo = Memory.Read(pc + 1);
        Registers.WriteI((hi << 8) | lo);
        AdvanceProgramCounter();
    }

    // ---- FN01 : select bitplane mask ----------------------------------------

    internal void SelectPlane(in DecodedOp op)
    {
        Display.SelectedPlanes = (byte)op.X;
    }

    // ---- F002 / FX3A : audio pattern buffer + pitch -------------------------

    internal void LoadAudioPattern(in DecodedOp op)
    {
        _audio.WritePattern(span =>
        {
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = Memory.Read(Registers.ReadIWithOffset(i));
            }
        });
    }

    internal void SetPitch(in DecodedOp op)
    {
        _audio.Pitch = Registers.ReadV(op.X);
    }

    // ---- 5XY2 / 5XY3 : store / load register range --------------------------

    internal void StoreRegisterRange(in DecodedOp op)  // 5XY2
    {
        var step = op.X <= op.Y ? 1 : -1;
        var count = Math.Abs(op.Y - op.X) + 1;
        for (var k = 0; k < count; k++)
        {
            Memory.Write(
                Registers.ReadIWithOffset(k),
                Registers.ReadV(op.X + k * step));
        }
    }

    internal void LoadRegisterRange(in DecodedOp op)  // 5XY3
    {
        var step = op.X <= op.Y ? 1 : -1;
        var count = Math.Abs(op.Y - op.X) + 1;
        for (var k = 0; k < count; k++)
        {
            var address = Registers.ReadIWithOffset(k);
            var value = Memory.Read(address);
            Registers.WriteV(op.X + k * step, value);
        }
    }
}
