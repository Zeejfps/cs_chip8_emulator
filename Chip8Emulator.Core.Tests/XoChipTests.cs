using Chip8Emulator.Core;
using Chip8Emulator.Core.Impl;
using Chip8Emulator.Core.Tests.Fakes;

namespace Chip8Emulator.Core.Tests;

public class XoChipTests
{
    private const int LowResWidth = 64;

    private static Chip8Machine CreateEmulator(IPersistentFlags? flags = null)
        => new(new FakeRenderer(), new FakeAudio(), new FakeClock(), new FakeInput(),
            flags ?? new InMemoryPersistentFlags());

    private static Chip8Machine CreateEmulator(FakeAudio audio)
        => new(new FakeRenderer(), audio, new FakeClock(), new FakeInput(), new InMemoryPersistentFlags());

    private static byte PixelAt(Chip8Machine emulator, int x, int y)
        => emulator.Display.Pixels.Span[y * emulator.Display.Width + x];

    // ---- FX01 : select bitplane mask ----------------------------------------

    [Fact]
    public void SelectPlane_StoresPlaneMask()
    {
        var emulator = CreateEmulator();

        Chip8Cpu.ExecuteTimerIns(emulator, 0xF201); // mask = 2 (plane 1 only)

        Assert.Equal(2, emulator.SelectedPlanes);
    }

    [Fact]
    public void SelectPlane_ZeroMakesDrawsNoOp()
    {
        var emulator = CreateEmulator();
        emulator.WriteMemory(0x300, [0xFF]);
        Chip8Cpu.ExecuteSetIndexRegisterIns(emulator, 0xA300);

        Chip8Cpu.ExecuteTimerIns(emulator, 0xF001); // mask = 0
        Chip8Cpu.ExeuteDrawToScreenIns(emulator, 0xD001);

        for (var x = 0; x < 8; x++) Assert.Equal(0, PixelAt(emulator, x, 0));
        Assert.Equal(0, emulator.Debugger.Registers[0xF]);
    }

    [Fact]
    public void SelectPlane_PlaneOneDrawsIntoBit1()
    {
        var emulator = CreateEmulator();
        emulator.WriteMemory(0x300, [0xFF]);
        Chip8Cpu.ExecuteSetIndexRegisterIns(emulator, 0xA300);

        Chip8Cpu.ExecuteTimerIns(emulator, 0xF201); // mask = 2
        Chip8Cpu.ExeuteDrawToScreenIns(emulator, 0xD001);

        for (var x = 0; x < 8; x++) Assert.Equal(0x02, PixelAt(emulator, x, 0));
    }

    [Fact]
    public void SelectPlane_BothPlanesConsumeTwoByteRuns()
    {
        var emulator = CreateEmulator();
        // plane 0 sprite: 1 byte = 0xFF (all on)
        // plane 1 sprite: 1 byte = 0x0F (low nibble on)
        emulator.WriteMemory(0x300, [0xFF, 0x0F]);
        Chip8Cpu.ExecuteSetIndexRegisterIns(emulator, 0xA300);
        Chip8Cpu.ExecuteTimerIns(emulator, 0xF301); // mask = 3 (both planes)

        Chip8Cpu.ExeuteDrawToScreenIns(emulator, 0xD001);

        // Bits 0..3: plane 0 on AND plane 1 off -> 0x01
        // Bits 4..7: plane 0 on AND plane 1 on  -> 0x03
        for (var x = 0; x < 4; x++) Assert.Equal(0x01, PixelAt(emulator, x, 0));
        for (var x = 4; x < 8; x++) Assert.Equal(0x03, PixelAt(emulator, x, 0));
    }

    [Fact]
    public void SelectPlane_BothPlanesCollisionVfOne()
    {
        var emulator = CreateEmulator();
        emulator.WriteMemory(0x300, [0xFF, 0xFF]);
        Chip8Cpu.ExecuteSetIndexRegisterIns(emulator, 0xA300);
        Chip8Cpu.ExecuteTimerIns(emulator, 0xF301); // mask = 3

        Chip8Cpu.ExeuteDrawToScreenIns(emulator, 0xD001);
        Assert.Equal(0, emulator.Debugger.Registers[0xF]);

        Chip8Cpu.ExeuteDrawToScreenIns(emulator, 0xD001);
        Assert.Equal(1, emulator.Debugger.Registers[0xF]);
    }

    // ---- Clear / scroll respect plane mask ----------------------------------

    [Fact]
    public void Clear_OnlyClearsSelectedPlaneBits()
    {
        var emulator = CreateEmulator();
        // Set plane 0 pixels
        emulator.WriteMemory(0x300, [0xFF]);
        Chip8Cpu.ExecuteSetIndexRegisterIns(emulator, 0xA300);
        Chip8Cpu.ExeuteDrawToScreenIns(emulator, 0xD001);
        // Set plane 1 pixels on the same row
        Chip8Cpu.ExecuteTimerIns(emulator, 0xF201); // mask = 2
        Chip8Cpu.ExeuteDrawToScreenIns(emulator, 0xD001);

        // Now clear only plane 0
        Chip8Cpu.ExecuteTimerIns(emulator, 0xF101); // mask = 1
        Chip8Cpu.ExecuteZeroBaseIns(emulator, 0x00E0);

        // Plane 0 bits gone, plane 1 bits remain
        for (var x = 0; x < 8; x++) Assert.Equal(0x02, PixelAt(emulator, x, 0));
    }

    [Fact]
    public void ScrollRight_OnlyMovesSelectedPlane()
    {
        var emulator = CreateEmulator();
        emulator.WriteMemory(0x300, [0xFF]);
        Chip8Cpu.ExecuteSetIndexRegisterIns(emulator, 0xA300);

        // Draw into plane 0
        Chip8Cpu.ExeuteDrawToScreenIns(emulator, 0xD001);
        // Draw the same shape into plane 1
        Chip8Cpu.ExecuteTimerIns(emulator, 0xF201);
        Chip8Cpu.ExeuteDrawToScreenIns(emulator, 0xD001);

        // Scroll right by 4 but only plane 1
        Chip8Cpu.ExecuteZeroBaseIns(emulator, 0x00FB);

        // x=0..3 : plane 0 only (0x01)
        // x=4..7 : plane 0 and plane 1 (0x03)
        // x=8..11: plane 1 only (0x02)
        for (var x = 0; x < 4; x++) Assert.Equal(0x01, PixelAt(emulator, x, 0));
        for (var x = 4; x < 8; x++) Assert.Equal(0x03, PixelAt(emulator, x, 0));
        for (var x = 8; x < 12; x++) Assert.Equal(0x02, PixelAt(emulator, x, 0));
    }

    // ---- F002 : load audio pattern ------------------------------------------

    [Fact]
    public void LoadAudioPattern_CopiesSixteenBytesFromIndex()
    {
        var audio = new FakeAudio();
        var emulator = CreateEmulator(audio);
        var pattern = new byte[16];
        for (var i = 0; i < pattern.Length; i++) pattern[i] = (byte)(i + 1);
        emulator.WriteMemory(0x400, pattern);
        Chip8Cpu.ExecuteSetIndexRegisterIns(emulator, 0xA400);

        Chip8Cpu.ExecuteTimerIns(emulator, 0xF002);

        Assert.Equal(1, audio.SetPatternCount);
        Assert.Equal(pattern, audio.LastPattern);
    }

    [Fact]
    public void LoadAudioPattern_DefaultPitchIs4000Hz()
    {
        var audio = new FakeAudio();
        var emulator = CreateEmulator(audio);
        Chip8Cpu.ExecuteSetIndexRegisterIns(emulator, 0xA400);

        Chip8Cpu.ExecuteTimerIns(emulator, 0xF002);

        Assert.Equal(4000.0, audio.LastFrequencyHz, precision: 3);
    }

    [Fact]
    public void LoadAudioPattern_IgnoredWhenXIsNonZero()
    {
        var audio = new FakeAudio();
        var emulator = CreateEmulator(audio);

        Chip8Cpu.ExecuteTimerIns(emulator, 0xF102); // F102 is not F002

        Assert.Equal(0, audio.SetPatternCount);
    }

    // ---- FX3A : set audio pitch ---------------------------------------------

    [Fact]
    public void SetPitch_UpdatesFrequencyPerOctoSpec()
    {
        var audio = new FakeAudio();
        var emulator = CreateEmulator(audio);
        Chip8Cpu.ExecuteSetRegisterValueIns(emulator, 0x6070); // V0 = 112

        Chip8Cpu.ExecuteTimerIns(emulator, 0xF03A);

        // 4000 * 2^((112 - 64) / 48) = 4000 * 2 = 8000
        Assert.Equal(8000.0, audio.LastFrequencyHz, precision: 3);
    }

    [Fact]
    public void SetPitch_DefaultPitch64Produces4000Hz()
    {
        var audio = new FakeAudio();
        var emulator = CreateEmulator(audio);
        Chip8Cpu.ExecuteSetRegisterValueIns(emulator, 0x6040); // V0 = 64

        Chip8Cpu.ExecuteTimerIns(emulator, 0xF03A);

        Assert.Equal(4000.0, audio.LastFrequencyHz, precision: 3);
    }

    // ---- FX75 / FX85 : persistent user flags --------------------------------

    [Fact]
    public void SaveFlags_WritesV0ThroughVxToPersistentStorage()
    {
        var flags = new InMemoryPersistentFlags();
        var emulator = CreateEmulator(flags);
        Chip8Cpu.ExecuteSetRegisterValueIns(emulator, 0x60AA); // V0 = 0xAA
        Chip8Cpu.ExecuteSetRegisterValueIns(emulator, 0x61BB); // V1 = 0xBB
        Chip8Cpu.ExecuteSetRegisterValueIns(emulator, 0x62CC); // V2 = 0xCC

        Chip8Cpu.ExecuteTimerIns(emulator, 0xF275); // FX75 with X=2 saves V0..V2

        Span<byte> readBack = stackalloc byte[16];
        flags.Read(readBack);
        Assert.Equal(0xAA, readBack[0]);
        Assert.Equal(0xBB, readBack[1]);
        Assert.Equal(0xCC, readBack[2]);
    }

    [Fact]
    public void LoadFlags_RestoresV0ThroughVxFromPersistentStorage()
    {
        var flags = new InMemoryPersistentFlags();
        Span<byte> seed = stackalloc byte[16];
        seed[0] = 0x11; seed[1] = 0x22; seed[2] = 0x33; seed[3] = 0x44;
        flags.Write(seed);
        var emulator = CreateEmulator(flags);

        Chip8Cpu.ExecuteTimerIns(emulator, 0xF385); // FX85 with X=3 loads V0..V3

        Assert.Equal(0x11, emulator.Debugger.Registers[0]);
        Assert.Equal(0x22, emulator.Debugger.Registers[1]);
        Assert.Equal(0x33, emulator.Debugger.Registers[2]);
        Assert.Equal(0x44, emulator.Debugger.Registers[3]);
    }

    [Fact]
    public void SaveLoadFlags_RoundTripPreservesValues()
    {
        var flags = new InMemoryPersistentFlags();
        var emulator = CreateEmulator(flags);
        for (var i = 0; i < 16; i++)
        {
            Chip8Cpu.ExecuteSetRegisterValueIns(emulator, 0x6000 | (i << 8) | (i * 17));
        }

        Chip8Cpu.ExecuteTimerIns(emulator, 0xFF75); // save V0..VF

        var restored = CreateEmulator(flags);
        Chip8Cpu.ExecuteTimerIns(restored, 0xFF85); // load V0..VF

        for (var i = 0; i < 16; i++)
        {
            Assert.Equal((byte)(i * 17), restored.Debugger.Registers[i]);
        }
    }

    // ---- F000 NNNN : long load I (smoke test, already existed) --------------

    [Fact]
    public void LongLoadI_ReadsNextTwoBytesIntoIndexRegister()
    {
        var emulator = CreateEmulator();
        emulator.WriteMemory(0x400, [0x12, 0x34]);
        // Simulate the fetch/decode phase: PC points at the NNNN word after the F000 opcode.
        emulator.WriteProgramCounter(0x400);

        Chip8Cpu.ExecuteTimerIns(emulator, 0xF000);

        Assert.Equal(0x1234, emulator.Debugger.IndexRegister);
    }

    // ---- LoadProgram resets XO-Chip state -----------------------------------

    [Fact]
    public void LoadProgram_ResetsSelectedPlanesToPlaneZero()
    {
        var emulator = CreateEmulator();
        Chip8Cpu.ExecuteTimerIns(emulator, 0xF301); // mask = 3

        emulator.LoadProgram([0x00, 0xE0]);

        Assert.Equal(1, emulator.SelectedPlanes);
    }
}
