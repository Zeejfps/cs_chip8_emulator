using Chip8Emulator.Core.Impl;
using Chip8Emulator.Core.Tests.Fakes;

namespace Chip8Emulator.Core.Tests;

public class Chip8EmulatorBuilderTests
{
    [Fact]
    public void Builder_ReturnsNewBuilderInstance()
    {
        var builder = Chip8.Builder();

        Assert.NotNull(builder);
        Assert.IsType<Chip8EmulatorBuilder>(builder);
    }

    [Fact]
    public void WithDisplay_ReturnsSameBuilder()
    {
        var builder = Chip8.Builder();

        var result = builder.WithDisplay(new FakeDisplay());

        Assert.Same(builder, result);
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

        var result = builder.WithInput();

        Assert.Same(builder, result);
    }

    [Fact]
    public void Build_ReturnsChip8Emulator()
    {
        var chip = Chip8.Builder()
            .WithDisplay(new FakeDisplay())
            .WithAudio(new FakeAudio())
            .WithInput()
            .Build();

        Assert.NotNull(chip);
        Assert.IsType<Chip8Machine>(chip);
    }
}
