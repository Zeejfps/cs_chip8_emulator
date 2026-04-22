namespace Chip8Emulator.Core.Impl;

internal static class Cpu
{
    public static void ExecuteLoadRegisterRange(Chip8Machine machine, int ins)  // 5XY3       
    {                                  
        var x = ExtractX(ins);                                    
        var y = ExtractY(ins);
        var step = x <= y ? 1 : -1;                               
        var count = Math.Abs(y - x) + 1;                          
        for (var k = 0; k < count; k++)
        {
            var address = machine.ReadIndexRegisterWithOffset(k);
            var value = machine.ReadMemory(address);
            machine.WriteRegister(x + k * step, value);                      
        }
    }
    
    private static int ExtractX(int ins)
    {
        return Chip8Disassembler.ExtractX(ins);
    }
    
    private static int ExtractY(int ins)
    {
        return Chip8Disassembler.ExtractY(ins);
    }
}