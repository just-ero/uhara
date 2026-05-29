using System;
using System.Diagnostics;
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
            if (self.GetModule(name) is not { BaseAddress: nint start and not 0, ModuleMemorySize: int size and not 0 })
                return null;

            return TMemory.ReadMemoryBytes(self, start, size);
        }

        public nint GetProcAddress(string moduleName, string functionName)
        {
            if (self.GetModule(moduleName) is not { BaseAddress: nint start and not 0 })
                return 0;

            return GetProcAddress(self, start, functionName);
        }

        public nint GetProcAddress(nint moduleBase, string functionName)
        {
            byte[] searchNameBytes = TUtils.StringToMultibyte(functionName);

            nint rbx = moduleBase;
            nint rax = TMemory.ReadMemory<int>(self, moduleBase + 0x3C);
            rax += rbx; // ntHeader
            nint rcx = TMemory.ReadMemory<int>(self, rax + 0x88); // export RVA
            rcx += rbx; // exportDir absolute
            nint r10 = TMemory.ReadMemory<int>(self, rcx + 0x18); // NumberOfNames
            nint r11 = TMemory.ReadMemory<int>(self, rcx + 0x20); // AddressOfNames RVA
            r11 += rbx; // absolute
            nint r12 = TMemory.ReadMemory<int>(self, rcx + 0x24); // AddressOfNameOrdinals RVA
            r12 += rbx; // absolute
            nint r13 = TMemory.ReadMemory<int>(self, rcx + 0x1C); // AddressOfFunctions RVA
            r13 += rbx; // absolute
            nint rdx = 0;

            while (rdx < r10)
            {
                rax = TMemory.ReadMemory<int>(self, r11 + (rdx * 4)); // name RVA
                rax += rbx; // absolute ptr to name string

                byte[]? nameBytes = TMemory.ReadMemoryBytes(self, rax, searchNameBytes.Length);
                if (nameBytes != null && nameBytes.SequenceEqual(searchNameBytes))
                {
                    short ordinal = TMemory.ReadMemory<short>(self, r12 + (rdx * 2));
                    int funcRVA = TMemory.ReadMemory<int>(self, r13 + (ordinal * 4));
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
