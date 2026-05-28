using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

internal static class ProcessExtensions
{
    extension(Process self)
    {
        public static Process? TryGetProcessById(int processId)
        {
            try
            {
                return Process.GetProcessById(processId);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public string Token => $"{self.StartTime.ToUniversalTime().Ticks:X}-{self.Id:X}";
        public bool IsAlive => self is { HasExited: false, MainModule: not null };

        public void WaitTillSecondsOld(int seconds)
        {
            var currentTime = DateTime.Now;
            var processStartTime = self.StartTime.ToUniversalTime();

            var duration = currentTime - processStartTime;
            if (duration.TotalSeconds < seconds)
            {
                var waitTime = (int)((seconds - duration.TotalSeconds) * 1000);
                Thread.Sleep(waitTime);
            }
        }

        public byte[]? GetModuleBytes(string? name = null)
        {
            do
            {
                var mod = GetModule(self, name);

                ulong start = (ulong)mod.BaseAddress;
                if (start == 0)
                    break;
                ulong size = (ulong)mod.ModuleMemorySize;
                if (size == 0)
                    break;

                return TMemory.ReadMemoryBytes(self, start, (int)size);
            }
            while (false);
            return null;
        }

        public ulong GetProcAddress(string moduleName, string functionName)
        {
            do
            {
                ProcessModule? module = GetModule(self, moduleName);
                if (module == null)
                    break;

                ulong moduleBase = (ulong)module.BaseAddress;
                if (moduleBase == 0)
                    break;

                return GetProcAddress(self, moduleBase, functionName);
            }
            while (false);
            return 0;
        }

        public ulong GetProcAddress(ulong moduleBase, string functionName)
        {
            byte[] searchNameBytes = TUtils.StringToMultibyte(functionName);

            ulong rbx = moduleBase;
            ulong rax = TMemory.ReadMemory<uint>(self, moduleBase + 0x3C);
            rax += rbx; // ntHeader
            ulong rcx = TMemory.ReadMemory<uint>(self, rax + 0x88); // export RVA
            rcx += rbx; // exportDir absolute
            ulong r10 = TMemory.ReadMemory<uint>(self, rcx + 0x18); // NumberOfNames
            ulong r11 = TMemory.ReadMemory<uint>(self, rcx + 0x20); // AddressOfNames RVA
            r11 += rbx; // absolute
            ulong r12 = TMemory.ReadMemory<uint>(self, rcx + 0x24); // AddressOfNameOrdinals RVA
            r12 += rbx; // absolute
            ulong r13 = TMemory.ReadMemory<uint>(self, rcx + 0x1C); // AddressOfFunctions RVA
            r13 += rbx; // absolute
            ulong rdx = 0;

            while (rdx < r10)
            {
                rax = TMemory.ReadMemory<uint>(self, r11 + (rdx * 4)); // name RVA
                rax += rbx; // absolute ptr to name string

                byte[]? nameBytes = TMemory.ReadMemoryBytes(self, rax, searchNameBytes.Length);
                if (nameBytes != null && nameBytes.SequenceEqual(searchNameBytes))
                {
                    ulong ordinal = TMemory.ReadMemory<ushort>(self, r12 + (rdx * 2));
                    ulong funcRVA = TMemory.ReadMemory<uint>(self, r13 + (ordinal * 4));
                    return rbx + funcRVA;
                }

                rdx++;
            }

            return 0;
        }

        public ProcessModule? GetModule(string? name = null)
        {
            if (self == null)
                return null;

            if (name == null)
            {
                return self.MainModule;
            }

            foreach (ProcessModule module in self.Modules)
            {
                if (module.ModuleName.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return module;
                }
            }

            return null;
        }

        public nint CreateRemoteThread(ulong entryPointAddress, int waitForThread = 0)
        {
            nint remoteThread = TImports.CreateRemoteThread(self.Handle, 0, 0, (nint)entryPointAddress, 0, 0, out _);

            if (waitForThread != 0)
                WaitForThread(remoteThread, waitForThread);

            return remoteThread;
        }

        public static bool WaitForThread(nint threadHandle, int timeout = -1)
        {
            return TImports.WaitForSingleObject(threadHandle, (uint)timeout) == 0;
        }
    }

    extension(ProcessModule self)
    {
        public IntPtr EndAddress => self.BaseAddress + self.ModuleMemorySize;
    }
}
