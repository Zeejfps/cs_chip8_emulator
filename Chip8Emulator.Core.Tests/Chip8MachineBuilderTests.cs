using Chip8Emulator.Core.Tests.Fakes;

namespace Chip8Emulator.Core.Tests;

public class Chip8MachineBuilderTests
{
    [Fact]
    public void Builder_ReturnsNewBuilderInstance()
    {
        var builder = Chip8.Builder();

        Assert.NotNull(builder);
        Assert.IsType<Chip8MachineBuilder>(builder);
    }

    [Fact]
    public void WithAudio_ReturnsSameBuilder()
    {
        var builder = Chip8.Builder();

        var result = builder.WithAudio(new FakeAudio());

        Assert.Same(builder, result);
    }

    [Fact]
    public void WithInput_ReturnsSameBuilder()
    {
        var builder = Chip8.Builder();

        var result = builder.WithInput(new FakeInput());

        Assert.Same(builder, result);
    }

    [Fact]
    public void WithClock_ReturnsSameBuilder()
    {
        var builder = Chip8.Builder();

        var result = builder.WithClock(new FakeClock());

        Assert.Same(builder, result);
    }

    [Fact]
    public void Build_ReturnsChip8Emulator()
    {
        var chip = Chip8.Builder()
            .WithDisplay(new EmulatedDisplay(size => new byte[size]))
            .WithAudio(new FakeAudio())
            .WithClock(new FakeClock())
            .WithInput(new FakeInput())
            .WithStack(new EmulatedStack(size => new int[size]))
            .WithMemory(new EmulatedMemory(size => new byte[size]))
            .WithRegisters(new EmulatedRegisters(size => new byte[size]))
            .Build();

        Assert.NotNull(chip);
        Assert.IsType<Chip8Interpreter>(chip);
    }
}
