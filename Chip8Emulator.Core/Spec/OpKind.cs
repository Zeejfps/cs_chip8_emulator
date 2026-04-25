namespace Chip8Emulator.Core.Spec;

internal enum OpKind
{
    Unknown = 0,

    // 0x0***
    Cls,                // 00E0
    Ret,                // 00EE
    ScrollDown,         // 00CN  (S-CHIP)
    ScrollUp,           // 00DN  (XO-CHIP)
    ScrollRight,        // 00FB  (S-CHIP)
    ScrollLeft,         // 00FC  (S-CHIP)
    DisableHires,       // 00FE  (S-CHIP)
    EnableHires,        // 00FF  (S-CHIP)

    // 0x1*** .. 0x4***
    Jp,                 // 1NNN
    Call,               // 2NNN
    SeVxImm,            // 3XNN
    SneVxImm,           // 4XNN

    // 0x5XY*
    SeVxVy,             // 5XY0
    StoreRegisterRange, // 5XY2  (XO-CHIP)
    LoadRegisterRange,  // 5XY3  (XO-CHIP)

    // 0x6*** .. 0x7***
    LdVxImm,            // 6XNN
    AddVxImm,           // 7XNN

    // 0x8XY*
    LdVxVy,             // 8XY0
    OrVxVy,             // 8XY1
    AndVxVy,            // 8XY2
    XorVxVy,            // 8XY3
    AddVxVy,            // 8XY4
    SubVxVy,            // 8XY5
    ShrVx,              // 8XY6
    SubnVxVy,           // 8XY7
    ShlVx,              // 8XYE

    // 0x9*** .. 0xC***
    SneVxVy,            // 9XY0
    LdIImm,             // ANNN
    JpV0,               // BNNN  (BXNN under quirk)
    Rnd,                // CXNN

    // 0xD***
    Drw,                // DXYN

    // 0xEX**
    Skp,                // EX9E
    Sknp,               // EXA1

    // 0xFX**
    LongLoadI,          // F000 NNNN  (XO-CHIP)
    SelectPlane,        // FN01       (XO-CHIP)
    LoadAudioPattern,   // F002       (XO-CHIP)
    LdVxDt,             // FX07
    LdVxK,              // FX0A
    LdDtVx,             // FX15
    LdStVx,             // FX18
    AddIVx,             // FX1E
    LdFVx,              // FX29
    LdHfVx,             // FX30  (S-CHIP)
    LdBVx,              // FX33
    SetPitch,           // FX3A  (XO-CHIP)
    LdIVx,              // FX55
    LdVxI,              // FX65
    SaveFlags,          // FX75  (S-CHIP)
    LoadFlags,          // FX85  (S-CHIP)
}
