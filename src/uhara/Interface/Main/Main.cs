using LiveSplit.ASL;
using LiveSplit.ComponentUtil;
using LiveSplit.Model;
using LiveSplit.UI.Components;
using LiveSplit.View;
using SharpDisasm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;

public partial class Main
{
    public static string LIB_VERSION = "10";

    public ScriptSettings Settings = new();
    public FileLogger FileLogger = new();

    // ---
    internal static LiveSplitState? CurrentState;

    internal static ulong UpdateCounter = 0;
    internal static string? UniqueScriptLoadID;

    internal class CountableStyle
    {
        internal static readonly int None = 0;
        internal static readonly int Array = 1;
        internal static readonly int List = 2;
    }

    internal static List<MemoryWatcher> MemoryWatchers = [];
    internal static List<(int style, Type type, MemoryWatcher memoryWatcher, DeepPointer deepPointer)> CountableWatchers = [];
    internal static List<StringWatcher> StringWatchers = [];

    internal static IDictionary<string, object>? Vars;
    public static bool DebugMode = true;

    internal static ulong LastStartTime;

    // ---
    internal static dynamic? _settings;

    internal static ASLScript? _script
    {
        get
        {
            if (field == null)
                CheckSetProcessAndValues();

            return field;
        }
        set;
    }

    internal static Process? ProcessInstance
    {
        get
        {
            if (field is { HasExited: false })
                return field;

            field = null;

            if (_script.GetFieldValue<Process>("_game") is not { } gameInstance)
                return field;

            if (Process.TryGetProcessById(gameInstance.Id) is not { } tempProcess)
                return field;

            if (tempProcess.Token == gameInstance.Token)
                field = tempProcess;

            return field;
        }
        private set => field = value is not null ? Process.TryGetProcessById(value.Id) : null;
    }

    internal static IDictionary<string, object>? current => field ??= _script?.State?.Data;
    internal static IDictionary<string, object>? old => field ??= _script?.OldState?.Data;

    public Main()
    {
        try
        {
            Thread.Sleep(50);
            TSaves2.Register("rumii", "uhara" + LIB_VERSION);
            UniqueScriptLoadID = TUtils.GenerateRandomString(32);
            TSaves2.Set(UniqueScriptLoadID, "IDs", "UniqueScriptLoadID");

            CheckSetProcessAndValues();

            // ---
            Vars?["Uhara"] = this;
            Vars?["Resolver"] = new PtrResolver();
        }
        catch { }
    }

    public Instruction[]? Disassemble(byte[] bytes)
    {
        try
        {
            return Disassemble(bytes, 0);
        }
        catch { }

        return null;
    }

    public Instruction[]? Disassemble(byte[] bytes, nint address)
    {
        try
        {
            return TInstruction.GetInstructions2(bytes, (ulong)address);
        }
        catch { }

        return null;
    }

    public static void AddWatcher(MemoryWatcher watcher)
    {
        MemoryWatchers.Add(watcher);
    }

    public static void AddWatcher<T>(DeepPointer deepPointer) where T : unmanaged
    {
        MemoryWatchers.Add(new MemoryWatcher<T>(deepPointer));
    }

    public static void AddWatcher<T>(nint baseAddress, params int[] offsets) where T : unmanaged
    {
        MemoryWatchers.Add(new MemoryWatcher<T>(new DeepPointer(baseAddress, offsets)));
    }

    [MemberNotNullWhen(true, nameof(ProcessInstance))]
    internal static bool ReloadProcess()
    {
        if (ProcessInstance is not { HasExited: false })
            return false;

        string lastName = ProcessInstance.ProcessName;
        string lastToken = ProcessInstance.Token;

        ProcessInstance = Process.TryGetProcessById(ProcessInstance.Id);

        if (ProcessInstance is not { HasExited: false })
            return false;

        string currentName = ProcessInstance.ProcessName;
        string currentToken = ProcessInstance.Token;

        return currentName == lastName
            && currentToken == lastToken;
    }

    [MemberNotNull(nameof(CurrentState))]
    internal static void CheckSetProcessAndValues()
    {
        if (Application.OpenForms[nameof(TimerForm)] is not TimerForm timerForm)
            return;

        _script = null;
        CurrentState = timerForm.CurrentState;

        if (CurrentState.Run?.AutoSplitter is { IsActivated: true, Component: ASLComponent cmp })
        {
            _script = cmp.Script;
        }
        else
        {
            foreach (var cmp2 in CurrentState.Layout.Components.OfType<ASLComponent>())
            {
                _script = cmp2.Script;
                if (_script != null)
                    break;
            }
        }

        if (_script != null)
        {
            Vars = _script.Vars;
            _settings = _script.GetFieldValue<ASLSettings>("_settings");
        }
    }

    internal static void SetProcessCache(string id, string name, string data)
    {
        string? token = ProcessInstance?.Token;
        if (string.IsNullOrEmpty(token))
            return;

        TSaves2.Set(data, "ProcessCache", id, name);
        TSaves2.Set(token!, "ProcessCache", id, name, "Token");
    }

    internal static string? GetProcessCache(string id, string name)
    {
        string? token = ProcessInstance?.Token;
        if (string.IsNullOrEmpty(token))
            return null;

        string? data = TSaves2.Get("ProcessCache", id, name);
        if (string.IsNullOrEmpty(data))
            return null;

        string? dataToken = TSaves2.Get("ProcessCache", id, name, "Token");
        if (string.IsNullOrEmpty(dataToken))
            return null;

        if (token == dataToken)
            return data;

        return null;
    }

    public MemoryWatcher? this[string key] => MemoryWatchers.FirstOrDefault(m => m.Name == key)
        ?? StringWatchers.FirstOrDefault(m => m.Name == key);

    public void ForceCleanMemory()
    {
        _ = ReloadProcess();
        MemoryManager.ClearMemory();
    }

    public bool IsModuleLoaded(string moduleName)
    {
        _ = ReloadProcess();
        return ProcessInstance?.GetModule(moduleName) is { BaseAddress: nint and not 0 };
    }

    public bool Is64Bit()
    {
        return ProcessInstance.Is64Bit();
    }

    public void AcceptOnFound(string signature)
    {
        string? processName = ProcessInstance?.ProcessName;
        if (string.IsNullOrEmpty(processName))
            return;

        bool found = false;
        foreach (Process process in Process.GetProcessesByName(processName))
        {
            if (!found)
            {
                ulong result = TMemory.ScanSingle(process, signature);
                if (result != 0)
                {
                    _script.SetFieldValue("_game", process);
                    found = true;
                    continue;
                }
            }

            process.Dispose();
        }

        _script.SetFieldValue<Process>("_game", null);
    }

    public bool Reject(bool condition = true)
    {
        try
        {
            if (condition)
            {
                _script.SetFieldValue<Process>("_game", null);
            }
        }
        catch { }

        return true;
    }

    public bool Reject(params int[] moduleMemorySizes)
    {
        try
        {
            return Reject(ProcessInstance.MainModule, moduleMemorySizes);
        }
        catch { }

        return false;
    }

    public bool Reject(string module, params int[] moduleMemorySizes)
    {
        try
        {
            return Reject(ProcessInstance.GetModule(module), moduleMemorySizes);
        }
        catch { }

        return false;
    }

    public bool Reject(ProcessModule module, params int[] moduleMemorySizes)
    {
        try
        {
            if (ProcessInstance == null)
            {
                TUtils.Print("Process not loaded yet");
                return false;
            }

            if (module is null)
            {
                TUtils.Print("Module could not be found");
                return false;
            }

            if (moduleMemorySizes is null || moduleMemorySizes.Length == 0)
            {
                _script.SetFieldValue<Process>("_game", null);
                return true;
            }

            int exeModuleSize = module.ModuleMemorySize;
            if (moduleMemorySizes.Any(mms => mms == exeModuleSize))
            {
                _script.SetFieldValue<Process>("_game", null);
                return true;
            }
        }
        catch { }

        return false;
    }

    public int GetImageSize(ProcessModule module)
    {
        try
        {
            return module.ModuleMemorySize;
        }
        catch { }

        return 0;
    }

    public int GetImageSize(string moduleName = null)
    {
        try
        {
            return ProcessInstance.GetModule(moduleName).ModuleMemorySize;
        }
        catch { }

        return 0;
    }

    public string GetMD5Hash(string modulePath = null)
    {
        try
        {
            if (string.IsNullOrEmpty(modulePath) || !File.Exists(modulePath))
                modulePath = ProcessInstance.MainModule.FileName;
            return GetHash(modulePath);
        }
        catch { }

        return null;
    }

    public string GetHash(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return null;

            byte[] bytes = [];
            using var md5 = MD5.Create();
            using var file = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            bytes = md5.ComputeHash(file);

            return bytes.Select(x => $"{x:X2}").Aggregate((a, b) => a + b);
        }
        catch { }

        return null;
    }

    public string GetHashRelative(string filePath)
    {
        try
        {
            do
            {
                string mainModulePath = ProcessInstance.MainModule.FileName;
                if (string.IsNullOrEmpty(mainModulePath))
                    break;

                string mainModuleDir = Path.GetDirectoryName(mainModulePath);
                filePath = Path.Combine(mainModuleDir, filePath);

                if (!File.Exists(filePath))
                    return null;

                byte[] bytes = [];
                using (var md5 = MD5.Create())
                {
                    using var file = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    bytes = md5.ComputeHash(file);
                }

                return bytes.Select(x => $"{x:X2}").Aggregate((a, b) => a + b);
            }
            while (false);
        }
        catch { }

        return null;
    }

    public void Log(string message)
    {
        try
        {
            if (!string.IsNullOrEmpty(message))
                TImports.OutputDebugString("[UHARA] " + message);
            else
                TImports.OutputDebugString("[UHARA] " + "Trying to print null");
        }
        catch { }
    }

    public void AlertLoadless()
    {
        try
        {
            if (CurrentState.CurrentTimingMethod != TimingMethod.GameTime)
            {
                if (MessageBox.Show("This autosplitter is using load removal and recommends using GameTime comparison, would you like to switch to it?", "LiveSplit",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    CurrentState.CurrentTimingMethod = TimingMethod.GameTime;
                }
            }
        }
        catch { }
    }

    public void AlertGameTime()
    {
        try
        {
            if (CurrentState.CurrentTimingMethod != TimingMethod.GameTime)
            {
                if (MessageBox.Show("This autosplitter recommends using GameTime comparison, would you like to switch to it?", "LiveSplit",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    CurrentState.CurrentTimingMethod = TimingMethod.GameTime;
                }
            }
        }
        catch { }
    }

    public void AlertRealTime()
    {
        try
        {
            if (CurrentState.CurrentTimingMethod != TimingMethod.RealTime)
            {
                if (MessageBox.Show("This autosplitter recommends using RealTime comparison, would you like to switch to it?", "LiveSplit",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    CurrentState.CurrentTimingMethod = TimingMethod.RealTime;
                }
            }
        }
        catch { }
    }

    public void DisableDebug()
    {
        try
        {
            DebugMode = false;
        }
        catch { }
    }

    public void EnableDebug()
    {
        try
        {
            DebugMode = true;
        }
        catch { }
    }

    private void TimerStartedAction()
    {

    }

    public void Update()
    {
        UpdateCounter++;

        /*do
        {
            ulong newAttemptStartTime = 0;
            DateTime? dt = CurrentState?.AttemptStarted.Time;
            if (dt.Value.Year >= 1601) newAttemptStartTime = dt != null && dt.HasValue ? (ulong)dt.Value.ToFileTime() : 0;
            if (newAttemptStartTime == 0) break;

            bool flag = newAttemptStartTime != LastStartTime;
            LastStartTime = newAttemptStartTime;

            if (flag) TimerStartedAction();
        }
        while (false);*/

        try
        {
            do
            {
                //if (ProcessInstance != null && !ProcessInstance.HasExited && ProcessInstance.Handle != 0)
                if (true)
                {
                    foreach (var watcher in MemoryWatchers)
                    {
                        old[watcher.Name] = watcher.Current;
                        watcher.Update(ProcessInstance);
                        current[watcher.Name] = watcher.Current;
                    }

                    foreach (var watcher in StringWatchers)
                    {
                        old[watcher.Name] = watcher.Current;
                        watcher.Update(ProcessInstance);
                        current[watcher.Name] = watcher.Current;
                    }

                    foreach (var watcher in CountableWatchers)
                    {
                        Type type = watcher.type;
                        bool success = false;

                        do
                        {
                            MemoryWatcher memWatcher = watcher.memoryWatcher;

                            memWatcher.Update(ProcessInstance);
                            if (memWatcher.Current == null)
                                break;
                            if ((nint)memWatcher.Current == 0)
                                break;

                            ulong listAddr = (ulong)(nint)memWatcher.Current;
                            if (listAddr == 0)
                                break;

                            int itemSize = Marshal.SizeOf(type);
                            int count = TMemory.ReadMemory<ushort>(ProcessInstance, listAddr + 0x18);
                            int size = count * itemSize;

                            ulong listItemsAddr = 0;
                            if (watcher.style == CountableStyle.Array)
                                listItemsAddr = listAddr;
                            else if (watcher.style == CountableStyle.List)
                                listItemsAddr = TMemory.ReadMemory<ulong>(ProcessInstance, listAddr + 0x10);

                            byte[] listBytes = TMemory.ReadMemoryBytes(ProcessInstance, listItemsAddr + 0x20, size);
                            if (listBytes == null || listBytes.Length == 0)
                                break;

                            // race safety check
                            ulong repeatListPtr = watcher.deepPointer.Deref<ulong>(ProcessInstance);
                            if (repeatListPtr != listAddr)
                                break;

                            // ---
                            if (type == typeof(nint))
                            {
                                var values = MemoryMarshal.Cast<byte, nint>(listBytes).ToArray();
                                if (watcher.style == CountableStyle.Array)
                                    current[watcher.memoryWatcher.Name] = values;
                                else if (watcher.style == CountableStyle.List)
                                    current[watcher.memoryWatcher.Name] = values.ToList();
                            }
                            else if (type == typeof(nuint))
                            {
                                var values = MemoryMarshal.Cast<byte, nuint>(listBytes).ToArray();
                                if (watcher.style == CountableStyle.Array)
                                    current[watcher.memoryWatcher.Name] = values;
                                else if (watcher.style == CountableStyle.List)
                                    current[watcher.memoryWatcher.Name] = values.ToList();
                            }
                            else if (type == typeof(bool))
                            {
                                var values = MemoryMarshal.Cast<byte, bool>(listBytes).ToArray();
                                if (watcher.style == CountableStyle.Array)
                                    current[watcher.memoryWatcher.Name] = values;
                                else if (watcher.style == CountableStyle.List)
                                    current[watcher.memoryWatcher.Name] = values.ToList();
                            }
                            else if (type == typeof(byte))
                            {
                                var values = listBytes;
                                if (watcher.style == CountableStyle.Array)
                                    current[watcher.memoryWatcher.Name] = values;
                                else if (watcher.style == CountableStyle.List)
                                    current[watcher.memoryWatcher.Name] = values.ToList();
                            }
                            else if (type == typeof(sbyte))
                            {
                                var values = MemoryMarshal.Cast<byte, sbyte>(listBytes).ToArray();
                                if (watcher.style == CountableStyle.Array)
                                    current[watcher.memoryWatcher.Name] = values;
                                else if (watcher.style == CountableStyle.List)
                                    current[watcher.memoryWatcher.Name] = values.ToList();
                            }
                            else if (type == typeof(char))
                            {
                                var values = MemoryMarshal.Cast<byte, char>(listBytes).ToArray();
                                if (watcher.style == CountableStyle.Array)
                                    current[watcher.memoryWatcher.Name] = values;
                                else if (watcher.style == CountableStyle.List)
                                    current[watcher.memoryWatcher.Name] = values.ToList();
                            }
                            else if (type == typeof(short))
                            {
                                var values = MemoryMarshal.Cast<byte, short>(listBytes).ToArray();
                                if (watcher.style == CountableStyle.Array)
                                    current[watcher.memoryWatcher.Name] = values;
                                else if (watcher.style == CountableStyle.List)
                                    current[watcher.memoryWatcher.Name] = values.ToList();
                            }
                            else if (type == typeof(ushort))
                            {
                                var values = MemoryMarshal.Cast<byte, ushort>(listBytes).ToArray();
                                if (watcher.style == CountableStyle.Array)
                                    current[watcher.memoryWatcher.Name] = values;
                                else if (watcher.style == CountableStyle.List)
                                    current[watcher.memoryWatcher.Name] = values.ToList();
                            }
                            else if (type == typeof(int))
                            {
                                var values = MemoryMarshal.Cast<byte, int>(listBytes).ToArray();
                                if (watcher.style == CountableStyle.Array)
                                    current[watcher.memoryWatcher.Name] = values;
                                else if (watcher.style == CountableStyle.List)
                                    current[watcher.memoryWatcher.Name] = values.ToList();
                            }
                            else if (type == typeof(uint))
                            {
                                var values = MemoryMarshal.Cast<byte, uint>(listBytes).ToArray();
                                if (watcher.style == CountableStyle.Array)
                                    current[watcher.memoryWatcher.Name] = values;
                                else if (watcher.style == CountableStyle.List)
                                    current[watcher.memoryWatcher.Name] = values.ToList();
                            }
                            else if (type == typeof(long))
                            {
                                var values = MemoryMarshal.Cast<byte, long>(listBytes).ToArray();
                                if (watcher.style == CountableStyle.Array)
                                    current[watcher.memoryWatcher.Name] = values;
                                else if (watcher.style == CountableStyle.List)
                                    current[watcher.memoryWatcher.Name] = values.ToList();
                            }
                            else if (type == typeof(ulong))
                            {
                                var values = MemoryMarshal.Cast<byte, ulong>(listBytes).ToArray();
                                if (watcher.style == CountableStyle.Array)
                                    current[watcher.memoryWatcher.Name] = values;
                                else if (watcher.style == CountableStyle.List)
                                    current[watcher.memoryWatcher.Name] = values.ToList();
                            }
                            else if (type == typeof(float))
                            {
                                var values = MemoryMarshal.Cast<byte, float>(listBytes).ToArray();
                                if (watcher.style == CountableStyle.Array)
                                    current[watcher.memoryWatcher.Name] = values;
                                else if (watcher.style == CountableStyle.List)
                                    current[watcher.memoryWatcher.Name] = values.ToList();
                            }
                            else if (type == typeof(double))
                            {
                                var values = MemoryMarshal.Cast<byte, double>(listBytes).ToArray();
                                if (watcher.style == CountableStyle.Array)
                                    current[watcher.memoryWatcher.Name] = values;
                                else if (watcher.style == CountableStyle.List)
                                    current[watcher.memoryWatcher.Name] = values.ToList();
                            }
                            else if (type == typeof(decimal))
                            {
                                var values = MemoryMarshal.Cast<byte, decimal>(listBytes).ToArray();
                                if (watcher.style == CountableStyle.Array)
                                    current[watcher.memoryWatcher.Name] = values;
                                else if (watcher.style == CountableStyle.List)
                                    current[watcher.memoryWatcher.Name] = values.ToList();
                            }

                            success = true;
                        }
                        while (false);

                        if (success)
                            continue;
                        else if (watcher.memoryWatcher.FailAction == MemoryWatcher.ReadFailAction.SetZeroOrNull)
                            current[watcher.memoryWatcher.Name] = null;
                        else if (watcher.style == CountableStyle.Array)
                        {
                            if (type == typeof(nint))
                                current[watcher.memoryWatcher.Name] = Array.Empty<nint>();
                            else if (type == typeof(nuint))
                                current[watcher.memoryWatcher.Name] = Array.Empty<nuint>();
                            else if (type == typeof(bool))
                                current[watcher.memoryWatcher.Name] = Array.Empty<bool>();
                            else if (type == typeof(byte))
                                current[watcher.memoryWatcher.Name] = Array.Empty<byte>();
                            else if (type == typeof(sbyte))
                                current[watcher.memoryWatcher.Name] = Array.Empty<sbyte>();
                            else if (type == typeof(char))
                                current[watcher.memoryWatcher.Name] = Array.Empty<char>();
                            else if (type == typeof(short))
                                current[watcher.memoryWatcher.Name] = Array.Empty<short>();
                            else if (type == typeof(ushort))
                                current[watcher.memoryWatcher.Name] = Array.Empty<ushort>();
                            else if (type == typeof(int))
                                current[watcher.memoryWatcher.Name] = Array.Empty<int>();
                            else if (type == typeof(uint))
                                current[watcher.memoryWatcher.Name] = Array.Empty<uint>();
                            else if (type == typeof(long))
                                current[watcher.memoryWatcher.Name] = Array.Empty<long>();
                            else if (type == typeof(ulong))
                                current[watcher.memoryWatcher.Name] = Array.Empty<ulong>();
                            else if (type == typeof(float))
                                current[watcher.memoryWatcher.Name] = Array.Empty<float>();
                            else if (type == typeof(double))
                                current[watcher.memoryWatcher.Name] = Array.Empty<double>();
                            else if (type == typeof(decimal))
                                current[watcher.memoryWatcher.Name] = Array.Empty<decimal>();
                        }
                        else if (watcher.style == CountableStyle.List)
                        {
                            if (type == typeof(nint))
                                current[watcher.memoryWatcher.Name] = new List<nint>();
                            else if (type == typeof(nuint))
                                current[watcher.memoryWatcher.Name] = new List<nuint>();
                            else if (type == typeof(bool))
                                current[watcher.memoryWatcher.Name] = new List<bool>();
                            else if (type == typeof(byte))
                                current[watcher.memoryWatcher.Name] = new List<byte>();
                            else if (type == typeof(sbyte))
                                current[watcher.memoryWatcher.Name] = new List<sbyte>();
                            else if (type == typeof(char))
                                current[watcher.memoryWatcher.Name] = new List<char>();
                            else if (type == typeof(short))
                                current[watcher.memoryWatcher.Name] = new List<short>();
                            else if (type == typeof(ushort))
                                current[watcher.memoryWatcher.Name] = new List<ushort>();
                            else if (type == typeof(int))
                                current[watcher.memoryWatcher.Name] = new List<int>();
                            else if (type == typeof(uint))
                                current[watcher.memoryWatcher.Name] = new List<uint>();
                            else if (type == typeof(long))
                                current[watcher.memoryWatcher.Name] = new List<long>();
                            else if (type == typeof(ulong))
                                current[watcher.memoryWatcher.Name] = new List<ulong>();
                            else if (type == typeof(float))
                                current[watcher.memoryWatcher.Name] = new List<float>();
                            else if (type == typeof(double))
                                current[watcher.memoryWatcher.Name] = new List<double>();
                            else if (type == typeof(decimal))
                                current[watcher.memoryWatcher.Name] = new List<decimal>();
                        }
                    }
                }
            }
            while (false);
        }
        catch { }
    }

    public dynamic CreateTool(string engine, string tool)
    {
        return CreateTool(engine, "default", tool);
    }

    public object CreateTool(string engine, string type, string tool)
    {
        if (!ReloadProcess())
            throw new Exception();
        ProcessInstance.WaitTillSecondsOld(1);
        if (!ReloadProcess())
            throw new Exception();

        try
        {
            // unity
            if (ToolsShared.ToolNames.Unity.Data.Contains(engine, StringComparer.OrdinalIgnoreCase))
            {
                if (ToolsShared.ToolNames.Unity.DotNet.Data.Contains(type, StringComparer.OrdinalIgnoreCase))
                {
                    if (ToolsShared.ToolNames.Unity.DotNet.JitSave.Data.Contains(tool, StringComparer.OrdinalIgnoreCase))
                    {
                        return new Tools.Unity.DotNet.JitSave();
                    }

                    if (ToolsShared.ToolNames.Unity.DotNet.Instance.Data.Contains(tool, StringComparer.OrdinalIgnoreCase))
                    {
                        return new Tools.Unity.DotNet.Instance();
                    }
                }

                if (ToolsShared.ToolNames.Unity.Il2Cpp.Data.Contains(type, StringComparer.OrdinalIgnoreCase))
                {
                    if (ToolsShared.ToolNames.Unity.Il2Cpp.JitSave.Data.Contains(tool, StringComparer.OrdinalIgnoreCase))
                    {
                        return new Tools.Unity.IL2CPP.JitSave();
                    }

                    if (ToolsShared.ToolNames.Unity.Il2Cpp.Instance.Data.Contains(tool, StringComparer.OrdinalIgnoreCase))
                    {
                        return new Tools.Unity.IL2CPP.Instance();
                    }
                }

                if (ToolsShared.ToolNames.Unity.Default.Data.Contains(type, StringComparer.OrdinalIgnoreCase))
                {
                    if (ToolsShared.ToolNames.Unity.Default.GameObject.Data.Contains(tool, StringComparer.OrdinalIgnoreCase))
                    {
                        return new Tools.Unity.Default.GameObject();
                    }
                }

                if (ToolsShared.ToolNames.Unity.Utils.Data.Contains(tool, StringComparer.OrdinalIgnoreCase))
                {
                    return new Tools.Unity.Utilities();
                }
            }

            // unreal engine
            if (ToolsShared.ToolNames.UnrealEngine.Data.Contains(engine, StringComparer.OrdinalIgnoreCase))
            {
                if (ToolsShared.ToolNames.UnrealEngine.Default.Data.Contains(type, StringComparer.OrdinalIgnoreCase))
                {
                    if (ToolsShared.ToolNames.UnrealEngine.Default.Events.Data.Contains(tool, StringComparer.OrdinalIgnoreCase))
                    {
                        return new Tools.UnrealEngine.Default.Events();
                    }

                    if (ToolsShared.ToolNames.UnrealEngine.Default.Utilities.Data.Contains(tool, StringComparer.OrdinalIgnoreCase))
                    {
                        return new Tools.UnrealEngine.Default.Utilities();
                    }
                }
            }
        }
        catch
        {
            Thread.Sleep(500);
        }

        return null;
    }

    public void SetProcess(Process process)
    {
        ProcessInstance = Process.GetProcessById(process.Id);
    }
}
