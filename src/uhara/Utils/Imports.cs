using System.Runtime.InteropServices;

internal class TImports
{
    [DllImport("kernel32.dll")]
    public static extern bool CloseHandle(nint hObject);

    [DllImport("kernel32.dll")]
    public static extern nint OpenProcess(uint processAccess, bool bInheritHandle, int processId);

    [StructLayout(LayoutKind.Sequential)]
    public struct MBI
    {
        public nint BaseAddress;
        public nint AllocationBase;
        public uint AllocationProtect;
        public nint RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool VirtualQueryEx(nint hProcess, nint lpAddress, out MBI lpBuffer, uint dwLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool VirtualFreeEx(nint hProcess, nint lpAddress, int dwSize, uint dwFreeType);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint CreateRemoteThread(nint hProcess, nint lpThreadAttributes, uint dwStackSize, nint lpStartAddress, nint lpParameter, uint dwCreationFlags, out nint lpThreadId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint WaitForSingleObject(nint hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern unsafe bool ReadProcessMemory(
        nint hProcess,
        nint lpBaseAddress,
        void* lpBuffer,
        int nSize,
        out nint lpNumberOfBytesRead);

    [DllImport("kernel32.dll")]
    internal static extern void OutputDebugString(string lpOutputString);
}
