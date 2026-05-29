using SharpDisasm;
using SharpDisasm.Udis86;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

internal class TInstruction
{
    internal static Instruction[] GetInstructionsBackwards(Process process, nint address, int numBytes)
    {
        do
        {
            List<Instruction> instrs = [];

            nint behind = address - numBytes;
            byte[] bytes = TMemory.ReadMemoryBytes(process, behind, numBytes);
            if (bytes == null)
                break;

            instrs = [.. GetInstructions2(bytes, behind)];
            if (instrs == null)
                break;

            instrs.Reverse();
            return [.. instrs];
        }
        while (false);
        return [];
    }

    internal static nint GetAlignedAddress(Process process, nint address)
    {
        byte[] plank = TMemory.ReadMemoryBytes(process, address - 60, 100);
        Instruction[] insns = GetInstructions2(plank);
        nint toReturn = 0;

        nint current = address - 60;
        foreach (Instruction insn in insns)
        {
            if (current <= address && current + insn.Length > address)
            {
                toReturn = current;
                break;
            }

            current += insn.Length;
        }

        return toReturn;
    }

    internal static int ExtractRipValue(Instruction insn)
    {
        var op = insn.Operands.FirstOrDefault(op => op is { Type: ud_type.UD_OP_MEM, Base: ud_type.UD_R_RIP });
        if (op is null)
            return 0;

        return op.LvalSDWord;
    }

    internal static Instruction GetInstruction2(Process process, nint address)
    {
        byte[] bytes = TMemory.ReadMemoryBytes(process, address, 50);
        return Disassembler.Disassemble(bytes, address).First();
    }

    internal static Instruction GetInstruction2(byte[] bytes, nint address)
    {
        return Disassembler.Disassemble(bytes, address).First();
    }

    internal static Instruction[] GetInstructions2(byte[] bytes, nint address = 0)
    {
        return [.. Disassembler.Disassemble(bytes, address)];
    }

    internal static int GetMinimumOverwrite(Process process, nint address, int required = 5)
    {
        byte[] bytes = TMemory.ReadMemoryBytes(process, address, 50);
        return GetMinimumOverwrite(bytes, required);
    }

    internal static int GetMinimumOverwrite(string bytes, int required = 5)
    {
        return GetMinimumOverwrite(TSignature.GetBytes(bytes), required);
    }

    internal static int GetMinimumOverwrite(byte[] bytes, int required = 5)
    {
        int length = 0;
        foreach (Instruction instruction in Disassembler.Disassemble(bytes, 0))
        {
            length += instruction.Length;
            if (length >= required)
                return length;
        }

        return 0;
    }
}
