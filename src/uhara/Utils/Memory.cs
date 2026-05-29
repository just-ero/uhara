using LiveSplit.ComponentUtil;
using SharpDisasm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static TImports;

internal class TMemory
{
    internal static nint ScanSimple(Process process, string signature, string? moduleName = null)
    {
        do
        {
            ProcessModule? processModule = moduleName == null ? process.MainModule : process.GetModule(moduleName);
            if (processModule == null)
                break;

            nint modStart = processModule.BaseAddress;
            int modSize = processModule.ModuleMemorySize;

            byte[]? modBytes = ReadMemoryBytes(process, modStart, modSize);
            if (modBytes == null || modBytes.Length == 0)
                break;

            int offset = FindInArray(modBytes, signature);
            if (offset == -1)
                break;

            return modStart + offset;
        }
        while (false);
        return 0;
    }

    internal static nint DerefPointer(Process process, nint address, params int[] offsets)
    {
        do
        {
            if (address == 0)
                break;
            address = ReadMemory<nint>(process, address);
            if (address == 0)
                break;

            for (int i = 0; i < offsets.Length; i++)
            {
                address = ReadMemory<nint>(process, address + offsets[i]);
                if (address == 0)
                    return 0;
            }

            return address;
        }
        while (false);
        return 0;
    }

    internal static string ReadMemoryString(Process process, nint address, int maxLength)
    {
        byte[] textBytes = ReadMemoryBytes(process, address, maxLength);
        return TUtils.MultibyteToString(textBytes);
    }

    internal static nint[] ScanMultiple(Process process, string signature, string? moduleName = null, int memoryProtection = -1)
    {
        List<nint> resultsRaw = [];
        byte[]? searchBytes = TSignature.GetBytes(signature);
        string searchMask = TSignature.GetMask(signature);

        List<nint[]> sections = GetAllSections(process, moduleName);

        for (int i = 0; i < sections.Count; i++)
        {
            byte[]? sectionBytes = ReadMemoryBytes(process, sections[i][0], (int)sections[i][1]);
            if (sectionBytes != null && sectionBytes.Length > 0)
            {
                int searchOffset = 0;
                while (true)
                {
                    searchOffset = FindInArray(sectionBytes, searchBytes, searchMask, searchOffset);
                    if (searchOffset == -1)
                        break;

                    nint foundAddress = sections[i][0] + searchOffset;

                    if (memoryProtection == -1)
                        resultsRaw.Add(foundAddress);
                    else if (GetMemoryProtection(process, foundAddress) == memoryProtection)
                        resultsRaw.Add(foundAddress);

                    searchOffset += searchBytes.Length;
                    if (searchOffset >= sections[i][1])
                        break;
                }
            }
        }

        return [.. resultsRaw];
    }

    internal static int GetMemoryProtection(Process process, nint address, int size = 0x1000)
    {
        if (!VirtualQueryEx(process.Handle, address, out MBI mBi, (uint)size))
            return -1;
        return (int)mBi.Protect;
    }

    internal static int GetMinimumOverwriteBackwards(Process process, nint address, int overwrite)
    {
        nint hookAddress = address - 0x1000;
        byte[]? pageBytes = ReadMemoryBytes(process, hookAddress, 0x1000);
        Instruction[] instructions = TInstruction.GetInstructions2(pageBytes);

        List<int> insLengths = [];

        foreach (Instruction ins in instructions)
        {
            hookAddress += ins.Bytes.Length;
            insLengths.Add(ins.Bytes.Length);

            if (hookAddress == address)
                break;
        }

        int offset = 0;
        for (int i = insLengths.Count - 1; i >= 0; i--)
        {
            offset += insLengths[i];
            if (offset >= overwrite)
            {
                return offset;
            }
        }

        return 0;
    }

    internal static nint GetFunctionStart(Process process, nint address)
    {
        nint newAddress = address - 0x1000;
        byte[]? pageBytes = ReadMemoryBytes(process, newAddress, 0x1000);
        Instruction[] instructions = TInstruction.GetInstructions2(pageBytes, newAddress);

        int insIndex = 0;
        for (int i = 0; i < instructions.Length; i++)
        {
            newAddress += instructions[i].Bytes.Length;

            if (newAddress == address)
            {
                newAddress = instructions[i].Offset;
                insIndex = i;
                break;
            }
        }

        if (insIndex == 0)
            return 0;

        for (int i = insIndex; i >= 0; i--)
        {
            if (instructions[i].ToString() is "int3" or "ret")
            {
                return instructions[i].Offset + 1;
            }
        }

        return 0;
    }

    internal static nint GetFunctionReturn(Process process, nint functionAddress)
    {
        byte[]? pageBytes = ReadMemoryBytes(process, functionAddress, 0x1000);
        Instruction[] instructions = TInstruction.GetInstructions2(pageBytes);

        nint retAddress = functionAddress;
        int offset = 0;

        foreach (Instruction ins in instructions)
        {
            if (ins.ToString() == "ret")
                return retAddress + offset;
            else
                offset += ins.Bytes.Length;
        }

        return 0;
    }

    internal static byte[] ConvertRelativeToAbsolute(byte[] bytes, nint originalAddress)
    {
        Instruction[] instructions = TInstruction.GetInstructions2(bytes);
        List<byte[]> newBytes = [];

        int offset = 0;
        foreach (Instruction ins in instructions)
        {
            string txtIns = ins.ToString();

            if (ins.Bytes.Length == 5)
            {
                if (txtIns.StartsWith("call"))
                {
                    nint actualEnd = GetActualAddressFromRelative5ByteInstruction(ins.Bytes, originalAddress + offset);
                    newBytes.Add(GetAbsoluteCallBytes(actualEnd));
                }
                else if (txtIns.StartsWith("jmp"))
                {
                    nint actualEnd = GetActualAddressFromRelative5ByteInstruction(ins.Bytes, originalAddress + offset);
                    newBytes.Add(GetAbsoluteJumpBytes(actualEnd));
                }
                else
                    newBytes.Add(ins.Bytes);
            }
            else
                newBytes.Add(ins.Bytes);

            offset += ins.Bytes.Length;
        }

        return [.. newBytes.SelectMany(self => self)];
    }

    internal static nint GetActualAddressFromRelative5ByteInstruction(byte[] bytes, nint address)
    {
        Instruction instr = TInstruction.GetInstruction2(bytes, address);
        if (instr.Bytes.Length == 5)
        {
            int value = BitConverter.ToInt32(bytes, 1);
            return address + value + instr.Bytes.Length;
        }

        return 0;
    }

    internal static bool ConfirmBytes(Process process, nint address, string signature)
    {
        return ConfirmBytes(process, address, TSignature.GetBytes(signature));
    }

    internal static bool ConfirmBytes(Process process, nint address, byte[] bytes)
    {
        byte[]? read = ReadMemoryBytes(process, address, bytes.Length);

        if (read != null)
            return read.SequenceEqual(bytes);
        else
            return false;
    }

    internal static string GetSignature(byte[] array, bool noSpaces = false)
    {
        string hex = BitConverter.ToString(array);
        if (noSpaces)
            return hex.Replace("-", " ").Replace(" ", "");
        else
            return hex.Replace("-", " ");
    }

    internal static bool FreeMemory(Process process, nint address, int size, uint type = 0x00008000)
    {
        return VirtualFreeEx(process.Handle, address, size, type);
    }

    internal static byte[] GetAbsoluteJumpBytes(nint destination)
    {
        byte[] stub = [0xFF, 0x25, 0x00, 0x00, 0x00, 0x00];
        byte[] full = [.. stub, .. BitConverter.GetBytes(destination)];
        return full;
    }

    internal static nint CreateAbsoluteJump(Process process, nint source, nint destination)
    {
        byte[] stub = [0xFF, 0x25, 0x00, 0x00, 0x00, 0x00];
        byte[] full = [.. stub, .. BitConverter.GetBytes(destination)];
        process.WriteBytes(source, full);
        return full.Length;
    }

    internal static nint CreateAbsoluteCall(Process process, nint source, nint destination, byte rspArguments = 0)
    {
        byte[] subRsp = [0x48, 0x83, 0xEC, rspArguments];
        byte[] start = [0xEB, 0x08];
        byte[] address = BitConverter.GetBytes(destination);
        byte[] end = [0xFF, 0x15, 0xF2, 0xFF, 0xFF, 0xFF, 0x90];
        byte[] addRsp = [0x48, 0x83, 0xC4, rspArguments];

        byte[] full = rspArguments == 0
            ? [.. start, .. address, .. end]
            : [.. subRsp, .. start, .. address, .. end, .. addRsp];

        process.WriteBytes(source, full);
        return full.Length;
    }

    internal static byte[] GetAbsoluteCallBytes(nint destination)
    {
        byte[] start = [0xEB, 0x08];
        byte[] address = BitConverter.GetBytes(destination);
        byte[] end = [0xFF, 0x15, 0xF2, 0xFF, 0xFF, 0xFF, 0x90];
        return [.. start, .. address, .. end];
    }

    public static nint ScanAdvanced(Process process, TSignature.ScanData scanData, string? moduleName = null)
    {
        nint searchAddress = 0;

        List<nint[]> sections = GetAllSections(process, moduleName);
        List<byte[]> sectionsBytes = [];

        foreach (nint[] rSection in sections)
        {
            byte[] readBytes = ReadMemoryBytes(process, rSection[0], (int)rSection[1]);
            sectionsBytes.Add(readBytes);
        }

        List<KeyValuePair<string, int>> checkpoints = [.. scanData.Checkpoints];
        checkpoints.Insert(0, new(scanData.Signature, 0));

        // incorrect queen index check
        if (scanData.QueenCheckpointIndex < 0 || scanData.QueenCheckpointIndex >= checkpoints.Count)
            return 0;

        byte[]? baseByteSignature = TSignature.GetBytes(scanData.Signature);

        foreach (var section in sectionsBytes)
        {
            int searchOffset = 0;

            int offsetSuccess = 0;

            bool chainSuccess = false;
            int baseOffset = 0;

            for (int cpIdx = 0; cpIdx < checkpoints.Count; cpIdx++)
            {
                var cp = checkpoints[cpIdx];
                string[] separateSigs = cp.Key.Split(',');
                for (int sigIdx = 0; sigIdx < separateSigs.Length; sigIdx++)
                {
                    var sig = separateSigs[sigIdx];
                    byte[] searchBytes = TSignature.GetBytes(sig);
                    string searchMask = TSignature.GetMask(sig);

                    int maxDistance = cp.Value;
                    if (scanData.ReversedSearch && cpIdx != 0 && sigIdx == 0)
                    {
                        searchOffset -= cp.Value;
                        if (searchOffset <= 0 && Math.Abs(searchOffset) < searchBytes.Length)
                        {
                            chainSuccess = false;
                            break;
                        }
                    }

                    int searchOffset2 = FindInArray(section, searchBytes, searchMask, searchOffset, maxDistance);
                    chainSuccess = searchOffset2 != -1;

                    if (chainSuccess)
                    {
                        searchOffset = searchOffset2;
                        if (cpIdx == 0)
                            baseOffset = searchOffset2;
                        if (cpIdx == scanData.QueenCheckpointIndex)
                            offsetSuccess = searchOffset2;
                        break;
                    }
                    else
                        continue;
                }

                if (chainSuccess)
                    continue;
                else if (cpIdx == 0)
                    break;
                else
                {
                    searchOffset = baseOffset + baseByteSignature.Length;
                    if (searchOffset + baseByteSignature.Length > section[1])
                        break;
                    cpIdx = -1;
                }
            }

            if (chainSuccess)
            {
                searchAddress = section[0] + offsetSuccess;
                break;
            }
        }

        if (searchAddress != 0)
        {
            if (scanData.Relative)
            {
                nint searchAddressRelative = searchAddress + scanData.ToRelativeInstructionOffset;
                Instruction instr = TInstruction.GetInstruction2(process, searchAddressRelative);

                int value = TInstruction.ExtractRipValue(instr);
                return value != 0
                    ? searchAddressRelative + value + instr.Bytes.Length
                    : 0;
            }
            else if (scanData.FindStartFunction)
            {
                nint newAddress = TInstruction.GetAlignedAddress(process, searchAddress);
                byte[] disasm = ReadMemoryBytes(process, newAddress - 0x1000, 0x1000);
                Instruction[] instrs = TInstruction.GetInstructions2(disasm, newAddress - 0x1000);

                for (int i = instrs.Length - 1; i >= 0; i--)
                {
                    if (instrs[i].ToString() == "int3")
                    {
                        break;
                    }

                    newAddress -= instrs[i].Bytes.Length;
                }

                searchAddress = newAddress;
            }

            searchAddress += scanData.Offset;
        }

        return searchAddress;
    }

    internal static nint ScanSingle(Process process, string signature, string? moduleName = null, int memoryProtection = -1)
    {
        byte[] searchBytes = TSignature.GetBytes(signature);
        string searchMask = TSignature.GetMask(signature);

        foreach (var section in GetAllSections(process, moduleName))
        {
            var sectionBytes = ReadMemoryBytes(process, section[0], (int)section[1]);
            if (sectionBytes is not { Length: > 0 })
                continue;

            int searchOffset = 0;
            do
            {
                searchOffset = FindInArray(sectionBytes, searchBytes, searchMask, searchOffset);
                if (searchOffset == -1)
                    break;

                nint foundAddress = section[0] + searchOffset;
                if (memoryProtection == -1)
                    return foundAddress;
                else if (GetMemoryProtection(process, foundAddress) == memoryProtection)
                    return foundAddress;

                searchOffset += 1;
            } while ((searchOffset + searchBytes.Length - 1) < section[1]);
        }

        return 0;
    }

    internal static nint ScanRel(Process process, int offset, string signature)
    {
        byte[]? searchBytes = TSignature.GetBytes(signature);
        string searchMask = TSignature.GetMask(signature);

        List<nint[]> sections = GetAllSections(process);

        foreach (nint[] section in sections)
        {
            byte[] sectionBytes = ReadMemoryBytes(Main.ProcessInstance, section[0], (int)section[1]);
            if (sectionBytes != null && sectionBytes.Length > 0)
            {
                int searchOffset = FindInArray(sectionBytes, searchBytes, searchMask);
                if (searchOffset != -1)
                {
                    nint searchAddress = section[0] + searchOffset;

                    nint relativeAddress = searchAddress + offset;
                    int relativeValue = ReadMemory<int>(Main.ProcessInstance, relativeAddress);

                    return searchAddress + relativeValue + offset + 4;
                }
            }
        }

        return 0;
    }

    internal static nint ScanRel2(Process process, string signature, string? moduleName = null, int offset = 0)
    {
        byte[]? searchBytes = TSignature.GetBytes(signature);
        string searchMask = TSignature.GetMask(signature);

        nint searchAddress = ScanSingle(process, signature, moduleName);
        if (searchAddress == 0)
            return 0;

        searchAddress += offset;

        Instruction instr = TInstruction.GetInstruction2(process, searchAddress);
        int value = TInstruction.ExtractRipValue(instr);

        return value != 0
            ? searchAddress + value + instr.Bytes.Length
            : 0;
    }

    public static List<nint[]> GetAllSections(Process process, string? moduleName = null)
    {
        List<nint[]> sections = [];

        ProcessModule? procModule = process.GetModule(moduleName);
        if (procModule == null)
            return sections;

        nint baseAddress = procModule.BaseAddress;
        byte[]? peHeader = GetPEHeader(process, baseAddress);
        if (peHeader == null)
            return sections;

        int peHeaderOffset = BitConverter.ToInt32(peHeader, 0x3C);
        int sectionCount = BitConverter.ToInt16(peHeader, peHeaderOffset + 0x6);
        int optionalHeaderSize = BitConverter.ToInt16(peHeader, peHeaderOffset + 0x14);
        int sectionHeaderOffset = peHeaderOffset + 0x18 + optionalHeaderSize;

        for (int i = 0; i < sectionCount; i++)
        {
            int currentSectionOffset = sectionHeaderOffset + (i * 0x28);
            uint virtualSize = BitConverter.ToUInt32(peHeader, currentSectionOffset + 0x08);
            uint virtualAddress = BitConverter.ToUInt32(peHeader, currentSectionOffset + 0x0C);

            sections.Add([baseAddress + virtualAddress, virtualSize]);
        }

        if (sections.Count > 0)
            sections = [.. sections.OrderBy(x => x[0])];

        return sections;
    }

    internal static byte[]? GetPEHeader(Process process, nint baseAddress)
    {
        byte[]? dosHeader = ReadMemoryBytes(process, baseAddress, 0x40);
        if (dosHeader == null)
            return null;

        int peHeaderOffset = BitConverter.ToInt32(dosHeader, 0x3C);

        byte[]? peHeaderInfo = ReadMemoryBytes(process, baseAddress + peHeaderOffset, 0x18);
        if (peHeaderInfo == null)
            return null;

        int sectionCount = BitConverter.ToInt16(peHeaderInfo, 0x6);
        int optionalHeaderSize = BitConverter.ToInt16(peHeaderInfo, 0x14);

        int sectionHeaderOffset = peHeaderOffset + 0x18 + optionalHeaderSize;
        int totalSize = sectionHeaderOffset + (sectionCount * 0x28);

        return ReadMemoryBytes(process, baseAddress, totalSize);
    }

    internal static int FindInArray(byte[] chunkData, string signature, int startPosition = 0, int maxDistance = 0)
    {
        return FindInArray(chunkData, TSignature.GetBytes(signature), TSignature.GetMask(signature), startPosition, maxDistance);
    }

    internal static int FindInArray(byte[] chunkData, byte[] byteSignature, string mask = "", int startPosition = 0, int maxDistance = 0)
    {
        if (mask.Contains("<") || mask.Contains(">"))
            return FindInArrayWithHalfBytes(chunkData, byteSignature, mask, startPosition, maxDistance);

        int position = startPosition;
        bool maskOn = mask.Contains("?") || mask.Contains("!");
        int found = 0;
        bool flag = false;

        while (position < chunkData.Length)
        {
            flag = false;
            if (maskOn)
            {
                if ((mask[found] == 'x' && chunkData[position] == byteSignature[found]) ||
                (mask[found] == '?') || (mask[found] == '!' && chunkData[position] != byteSignature[found]))
                {
                    flag = true;
                }
            }
            else if (chunkData[position] == byteSignature[found])
                flag = true;

            if (flag)
            {
                found += 1;
                if (found == byteSignature.Length)
                    return position - found + 1;
            }
            else
            {
                position -= found - 1;
                found = 0;
            }

            if (flag)
                position += 1;
            if (maxDistance != 0 && position >= startPosition + maxDistance)
            {
                return -1;
            }
        }

        return -1;
    }

    private static int FindInArrayWithHalfBytes(byte[] chunkData, byte[] byteSignature, string mask = "", int startPosition = 0, int maxDistance = 0)
    {
        int position = startPosition;
        bool maskOn = true;
        int found = 0;
        bool flag = false;

        while (position < chunkData.Length)
        {
            flag = false;
            if (maskOn)
            {
                if ((mask[found] == 'x' && chunkData[position] == byteSignature[found]) ||
                (mask[found] == '?') ||
                (mask[found] == '<' && BitConverter.ToString([chunkData[position]])[1] == BitConverter.ToString([byteSignature[found]])[1]) ||
                (mask[found] == '>' && BitConverter.ToString([chunkData[position]])[0] == BitConverter.ToString([byteSignature[found]])[0]) ||
                (mask[found] == '!' && chunkData[position] != byteSignature[found]))
                {
                    flag = true;
                }
            }
            else if (chunkData[position] == byteSignature[found])
                flag = true;

            if (flag)
            {
                found += 1;
                if (found == byteSignature.Length)
                    return position - found + 1;
            }
            else
            {
                position -= found - 1;
                found = 0;
            }

            if (flag)
                position += 1;
            if (maxDistance != 0 && position >= startPosition + maxDistance)
            {
                return -1;
            }
        }

        return -1;
    }

    internal static unsafe T ReadMemory<T>(Process process, nint address) where T : unmanaged
    {
        T value;
        if (ReadProcessMemory(process.Handle, address, &value, sizeof(T), out _))
            return value;

        return default;
    }

    internal static unsafe byte[]? ReadMemoryBytes(Process process, nint address, int size)
    {
        byte[] data = new byte[size];
        fixed (byte* pData = data)
        {
            if (ReadProcessMemory(process.Handle, address, pData, data.Length, out _))
                return data;
        }

        return null;
    }
}
