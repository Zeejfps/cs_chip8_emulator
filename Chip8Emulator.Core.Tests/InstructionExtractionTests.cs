namespace Chip8Emulator.Core.Tests;

public class InstructionExtractionTests
{
    [Theory]
    [InlineData(0x1234, 0x234)]
    [InlineData(0xABCD, 0xBCD)]
    [InlineData(0x0000, 0x000)]
    [InlineData(0xFFFF, 0xFFF)]
    public void ExtractNnn_ReturnsLowerTwelveBits(int ins, int expected)
    {
        Assert.Equal(expected, Chip8Disassembler.ExtractNnn(ins));
    }

    [Theory]
    [InlineData(0x1234, 0x34)]
    [InlineData(0xABCD, 0xCD)]
    [InlineData(0x00FF, 0xFF)]
    [InlineData(0xFF00, 0x00)]
    public void ExtractNn_ReturnsLowerByte(int ins, byte expected)
    {
        Assert.Equal(expected, Chip8Disassembler.ExtractNn(ins));
    }

    [Theory]
    [InlineData(0x1234, 0x4)]
    [InlineData(0xABCD, 0xD)]
    [InlineData(0x000F, 0xF)]
    [InlineData(0xFFF0, 0x0)]
    public void ExtractN_ReturnsLowestNibble(int ins, int expected)
    {
        Assert.Equal(expected, Chip8Disassembler.ExtractN(ins));
    }

    [Theory]
    [InlineData(0x1234, 0x2)]
    [InlineData(0xABCD, 0xB)]
    [InlineData(0x0F00, 0xF)]
    [InlineData(0xF0FF, 0x0)]
    public void ExtractX_ReturnsSecondNibble(int ins, int expected)
    {
        Assert.Equal(expected, Chip8Disassembler.ExtractX(ins));
    }

    [Theory]
    [InlineData(0x1234, 0x3)]
    [InlineData(0xABCD, 0xC)]
    [InlineData(0x00F0, 0xF)]
    [InlineData(0xFF0F, 0x0)]
    public void ExtractY_ReturnsThirdNibble(int ins, int expected)
    {
        Assert.Equal(expected, Chip8Disassembler.ExtractY(ins));
    }
}
