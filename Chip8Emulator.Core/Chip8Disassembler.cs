namespace Chip8Emulator.Core;

public static class Chip8Disassembler
{
    public static string Disassemble(int ins) => Format(Chip8Decoder.Decode(ins));

    private static string Format(in DecodedOp op)
    {
        return op.Kind switch
        {
            OpKind.Cls => "CLS",
            OpKind.Ret => "RET",
            OpKind.ScrollDown => $"SCD  {op.N}",
            OpKind.ScrollUp => $"SCU  {op.N}",
            OpKind.ScrollRight => "SCR",
            OpKind.ScrollLeft => "SCL",
            OpKind.DisableHires => "LOW",
            OpKind.EnableHires => "HIGH",
            OpKind.Jp => $"JP   0x{op.Nnn:X3}",
            OpKind.Call => $"CALL 0x{op.Nnn:X3}",
            OpKind.SeVxImm => $"SE   V{op.X:X}, 0x{op.Nn:X2}",
            OpKind.SneVxImm => $"SNE  V{op.X:X}, 0x{op.Nn:X2}",
            OpKind.SeVxVy => $"SE   V{op.X:X}, V{op.Y:X}",
            OpKind.StoreRegisterRange => $"LD   [I], V{op.X:X}, V{op.Y:X}",
            OpKind.LoadRegisterRange => $"LD   V{op.X:X}, V{op.Y:X}, [I]",
            OpKind.LdVxImm => $"LD   V{op.X:X}, 0x{op.Nn:X2}",
            OpKind.AddVxImm => $"ADD  V{op.X:X}, 0x{op.Nn:X2}",
            OpKind.LdVxVy => $"LD   V{op.X:X}, V{op.Y:X}",
            OpKind.OrVxVy => $"OR   V{op.X:X}, V{op.Y:X}",
            OpKind.AndVxVy => $"AND  V{op.X:X}, V{op.Y:X}",
            OpKind.XorVxVy => $"XOR  V{op.X:X}, V{op.Y:X}",
            OpKind.AddVxVy => $"ADD  V{op.X:X}, V{op.Y:X}",
            OpKind.SubVxVy => $"SUB  V{op.X:X}, V{op.Y:X}",
            OpKind.ShrVx => $"SHR  V{op.X:X}",
            OpKind.SubnVxVy => $"SUBN V{op.X:X}, V{op.Y:X}",
            OpKind.ShlVx => $"SHL  V{op.X:X}",
            OpKind.SneVxVy => $"SNE  V{op.X:X}, V{op.Y:X}",
            OpKind.LdIImm => $"LD   I, 0x{op.Nnn:X3}",
            OpKind.JpV0 => $"JP   V0, 0x{op.Nnn:X3}",
            OpKind.Rnd => $"RND  V{op.X:X}, 0x{op.Nn:X2}",
            OpKind.Drw => $"DRW  V{op.X:X}, V{op.Y:X}, {op.N}",
            OpKind.Skp => $"SKP  V{op.X:X}",
            OpKind.Sknp => $"SKNP V{op.X:X}",
            OpKind.LongLoadI => "LD   I, LONG",
            OpKind.SelectPlane => $"PLANE {op.X}",
            OpKind.LoadAudioPattern => "AUDIO",
            OpKind.LdVxDt => $"LD   V{op.X:X}, DT",
            OpKind.LdVxK => $"LD   V{op.X:X}, K",
            OpKind.LdDtVx => $"LD   DT, V{op.X:X}",
            OpKind.LdStVx => $"LD   ST, V{op.X:X}",
            OpKind.AddIVx => $"ADD  I, V{op.X:X}",
            OpKind.LdFVx => $"LD   F, V{op.X:X}",
            OpKind.LdHfVx => $"LD   HF, V{op.X:X}",
            OpKind.LdBVx => $"LD   B, V{op.X:X}",
            OpKind.SetPitch => $"PITCH V{op.X:X}",
            OpKind.LdIVx => $"LD   [I], V{op.X:X}",
            OpKind.LdVxI => $"LD   V{op.X:X}, [I]",
            OpKind.SaveFlags => $"LD   R, V{op.X:X}",
            OpKind.LoadFlags => $"LD   V{op.X:X}, R",
            OpKind.Unknown => $"DW   0x{op.Raw:X4}",
            _ => $"DW   0x{op.Raw:X4}"
        };
    }
}
