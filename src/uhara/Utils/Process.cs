using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

internal class TProcess
{
    internal static bool IsSameProcess(Process process1, Process process2)
    {
        if (process1 == null || process2 == null)
            return false;
        else
            return GetToken(process1) == GetToken(process2);
    }

    internal static int GetImageSize(Process process, ProcessModule module)
    {
        module ??= process.MainModule;
        return module.ModuleMemorySize;
    }

    internal static int GetImageSize(Process process, string moduleName = null)
    {
        ProcessModule module = GetModule(process, moduleName);
        return GetImageSize(process, module);
    }

    internal static bool WaitTillSecondsOld(Process process, int seconds)
    {
        ulong currentTime = TUtils.GetTimeMiliseconds();
        ulong processStartTime = GetStartTimeMiliseconds(process);

        if (processStartTime != 0)
        {
            long waitTime = (long)(processStartTime + (ulong)(seconds * 1000) - currentTime);
            if (waitTime > 0)
                Thread.Sleep((int)waitTime);
            return true;
        }

        return false;
    }

    internal static ulong GetModuleEnd(Process process, string name = null)
    {
        if (name == null)
            return (ulong)process.MainModule.BaseAddress + (ulong)process.MainModule.ModuleMemorySize;
        else
        {
            ProcessModule module = GetModule(process, name);
            if (module == null)
                return 0;

            return (ulong)module.BaseAddress + (ulong)module.ModuleMemorySize;
        }
    }

    internal static byte[] GetModuleBytes(Process process, string name = null)
    {
        do
        {
            ulong start = GetModuleBase(process, name);
            if (start == 0)
                break;
            ulong size = GetModuleSize(process, name);
            if (size == 0)
                break;

            return TMemory.ReadMemoryBytes(process, start, (int)size);
        }
        while (false);
        return null;
    }

    public static ulong GetModuleSize(Process process, string name = null)
    {
        if (name == null)
            return (ulong)process.MainModule.ModuleMemorySize;
        return (ulong)GetModule(process, name).ModuleMemorySize;
    }

    internal static ulong GetModuleBase(Process process, string name = null)
    {
        if (name == null)
        {
            return (ulong)process.MainModule.BaseAddress;
        }
        else
        {
            foreach (ProcessModule module in process.Modules)
            {
                if (module.ModuleName.ToLower() == name.ToLower())
                {
                    return (ulong)module.BaseAddress;
                }
            }
        }

        return 0;
    }

    internal static ulong GetProcAddress(Process process, string moduleName, string functionName)
    {
        do
        {
            ProcessModule module = GetModule(process, moduleName);
            if (module == null)
                break;

            ulong moduleBase = (ulong)module.BaseAddress;
            if (moduleBase == 0)
                break;

            return GetProcAddress(process, moduleBase, functionName);
        }
        while (false);
        return 0;
    }

    internal static ulong GetProcAddress(Process process, ulong moduleBase, string functionName)
    {
        byte[] searchNameBytes = TUtils.StringToMultibyte(functionName);

        ulong rbx = moduleBase;
        ulong rax = TMemory.ReadMemory<uint>(process, moduleBase + 0x3C);
        rax += rbx; // ntHeader
        ulong rcx = TMemory.ReadMemory<uint>(process, rax + 0x88); // export RVA
        rcx += rbx; // exportDir absolute
        ulong r10 = TMemory.ReadMemory<uint>(process, rcx + 0x18); // NumberOfNames
        ulong r11 = TMemory.ReadMemory<uint>(process, rcx + 0x20); // AddressOfNames RVA
        r11 += rbx; // absolute
        ulong r12 = TMemory.ReadMemory<uint>(process, rcx + 0x24); // AddressOfNameOrdinals RVA
        r12 += rbx; // absolute
        ulong r13 = TMemory.ReadMemory<uint>(process, rcx + 0x1C); // AddressOfFunctions RVA
        r13 += rbx; // absolute
        ulong rdx = 0;

        while (rdx < r10)
        {
            rax = TMemory.ReadMemory<uint>(process, r11 + (rdx * 4)); // name RVA
            rax += rbx; // absolute ptr to name string

            byte[] nameBytes = TMemory.ReadMemoryBytes(process, rax, searchNameBytes.Length);
            if (nameBytes.SequenceEqual(searchNameBytes))
            {
                ulong ordinal = TMemory.ReadMemory<ushort>(process, r12 + (rdx * 2));
                ulong funcRVA = TMemory.ReadMemory<uint>(process, r13 + (ordinal * 4));
                return rbx + funcRVA;
            }

            rdx++;
        }

        return 0;
    }

    internal static ProcessModule GetModule(Process process, string name = null)
    {
        if (process == null)
            return null;

        if (name == null)
        {
            return process.MainModule;
        }
        else
        {
            foreach (ProcessModule module in process.Modules)
            {
                if (module.ModuleName.ToLower() == name.ToLower())
                {
                    return module;
                }
            }
        }

        return null;
    }

    internal static string GetToken(Process process)
    {
        return GetStartTimeMiliseconds(process).ToString("X") + "-" + process.Id.ToString("X");
    }

    internal static ulong GetStartTimeSeconds(Process process)
    {
        return (ulong)((DateTimeOffset)process.StartTime).ToUnixTimeSeconds();
    }

    internal static ulong GetStartTimeMiliseconds(Process process)
    {
        return (ulong)((DateTimeOffset)process.StartTime).ToUnixTimeMilliseconds();
    }

    internal static nint CreateRemoteThread(Process process, ulong entryPointAddress, int waitForThread = 0)
    {
        nint remoteThread = TImports.CreateRemoteThread(process.Handle, 0, 0,
                    (nint)entryPointAddress, 0, 0, out _);

        if (waitForThread != 0)
            WaitForThread(remoteThread, waitForThread);

        return remoteThread;
    }

    internal static bool WaitForThread(nint threadHandle, int timeout = -1)
    {
        return TImports.WaitForSingleObject(threadHandle, (uint)timeout) == 0;
    }

    internal static bool IsAlive(Process process)
    {
        try
        {
            return
            process != null &&
            !process.HasExited &&
            process.MainModule != null;
        }
        catch { }

        return false;
    }
}
