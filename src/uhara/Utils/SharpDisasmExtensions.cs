using SharpDisasm;
using System.Collections.Generic;

internal static class SharpDisasmExtensions
{
    extension(Disassembler)
    {
        public static IEnumerable<Instruction> Disassemble(byte[] bytes, nint address)
        {
            return new Disassembler(bytes, ArchitectureMode.x86_64, (ulong)address, copyBinaryToInstruction: true).Disassemble();
        }
    }
}
