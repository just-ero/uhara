using LiveSplit.ComponentUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

public class PtrResolver
{
    #region WATCHERS_ACCESS
    public object this[string key]
    {
        get
        {
            try
            {
                MemoryWatcher watcher = Main.MemoryWatchers.FirstOrDefault(m => m.Name == key) ?? Main.StringWatchers.FirstOrDefault(m => m.Name == key);
                return watcher;
            }
            catch { }

            return null;
        }
    }
    #endregion

    #region DEREF
    public nint Deref((nint _base, int[] offsets) offsets)
    {
        return _Deref(offsets._base, offsets: offsets.offsets);
    }

    public nint Deref(object _base, params int[] offsets)
    {
        return _Deref(_base, offsets: offsets);
    }

    public nint Deref(string moduleName, object _base, params int[] offsets)
    {
        return _Deref(_base, moduleName, offsets);
    }

    public nint Deref(Module module, object _base, params int[] offsets)
    {
        return _Deref(_base, module.Name, offsets);
    }

    public nint Deref(object _base, string moduleName = null, params int[] offsets)
    {
        return _Deref(_base, moduleName, offsets);
    }

    private nint _Deref(object _base, string moduleName = null, params int[] offsets)
    {
        try
        {
            int addition = 0;
            if (offsets.Length > 0)
            {
                addition = offsets[^1];
                List<int> modified = [.. offsets];
                modified.RemoveAt(modified.Count - 1);
                offsets = [.. modified];
            }

            DeepPointer deepPointer = _base switch
            {
                int i => string.IsNullOrEmpty(moduleName)
                    ? new DeepPointer(i, offsets)
                    : new DeepPointer(moduleName, i, offsets),
                nint p => new DeepPointer(p, offsets),
                _ => new DeepPointer((nint)Convert.ToInt64(_base), offsets)
            };

            nint result = deepPointer.Deref<nint>(Main.ProcessInstance);
            if (result != 0)
                return result + addition;
        }
        catch { }

        return 0;
    }
    #endregion
    #region READ
    public T Read<T>((nint _base, int[] offsets) offsets) where T : unmanaged
    {
        return _Read<T>(offsets._base, offsets: offsets.offsets);
    }

    public T Read<T>(object _base, params int[] offsets) where T : unmanaged
    {
        return _Read<T>(_base, offsets: offsets);
    }

    public T Read<T>(string moduleName, object _base, params int[] offsets) where T : unmanaged
    {
        return _Read<T>(_base, moduleName, offsets);
    }

    public T Read<T>(Module module, object _base, params int[] offsets) where T : unmanaged
    {
        return _Read<T>(_base, module.Name, offsets);
    }

    public T Read<T>(object _base, string moduleName = null, params int[] offsets) where T : unmanaged
    {
        return _Read<T>(_base, moduleName, offsets);
    }

    private T _Read<T>(object _base, string moduleName = null, params int[] offsets) where T : unmanaged
    {
        try
        {
            DeepPointer deepPointer = _base switch
            {
                int i => string.IsNullOrEmpty(moduleName)
                    ? new DeepPointer(i, offsets)
                    : new DeepPointer(moduleName, i, offsets),
                nint p => new DeepPointer(p, offsets),
                _ => new DeepPointer((nint)Convert.ToInt64(_base), offsets)
            };

            return deepPointer.Deref<T>(Main.ProcessInstance);
        }
        catch { }

        return default;
    }
    #endregion
    #region TRY_READ
    public bool TryRead<T>(out T result, (nint _base, int[] offsets) offsets) where T : unmanaged
    {
        return _TryRead(out result, offsets._base, offsets: offsets.offsets);
    }

    public bool TryRead<T>(out T result, object _base, params int[] offsets) where T : unmanaged
    {
        return _TryRead(out result, _base, offsets: offsets);
    }

    public bool TryRead<T>(out T result, string moduleName, object _base, params int[] offsets) where T : unmanaged
    {
        return _TryRead(out result, _base, moduleName, offsets);
    }

    public bool TryRead<T>(out T result, Module module, object _base, params int[] offsets) where T : unmanaged
    {
        return _TryRead(out result, _base, module.Name, offsets);
    }

    public bool TryRead<T>(out T result, object _base, string moduleName = null, params int[] offsets) where T : unmanaged
    {
        return _TryRead(out result, _base, moduleName, offsets);
    }

    private bool _TryRead<T>(out T result, object _base, string moduleName = null, params int[] offsets) where T : unmanaged
    {
        try
        {
            DeepPointer deepPointer = _base switch
            {
                int i => string.IsNullOrEmpty(moduleName)
                    ? new DeepPointer(i, offsets)
                    : new DeepPointer(moduleName, i, offsets),
                nint p => new DeepPointer(p, offsets),
                _ => new DeepPointer((nint)Convert.ToInt64(_base), offsets)
            };

            if (deepPointer.Deref(Main.ProcessInstance, out T value))
            {
                result = value;
                return true;
            }
        }
        catch { }

        result = default;
        return false;
    }
    #endregion
    #region READ_ARRAY
    public T[] ReadArray<T>((nint _base, int[] offsets) offsets) where T : unmanaged
    {
        return _ReadArray<T>(typeof(T), offsets._base, offsets: offsets.offsets);
    }

    public T[] ReadArray<T>(object _base, params int[] offsets) where T : unmanaged
    {
        return _ReadArray<T>(typeof(T), _base, offsets: offsets);
    }

    public T[] ReadArray<T>(string moduleName, object _base, params int[] offsets) where T : unmanaged
    {
        return _ReadArray<T>(typeof(T), _base, moduleName, offsets);
    }

    public T[] ReadArray<T>(Module module, object _base, params int[] offsets) where T : unmanaged
    {
        return _ReadArray<T>(typeof(T), _base, module.Name, offsets);
    }

    public T[] ReadArray<T>(object _base, string moduleName = null, params int[] offsets) where T : unmanaged
    {
        return _ReadArray<T>(typeof(T), _base, moduleName, offsets);
    }

    private T[] _ReadArray<T>(Type type, object _base, string moduleName = null, params int[] offsets) where T : unmanaged
    {
        try
        {
            do
            {
                DeepPointer deepPointer = _base switch
                {
                    int i => string.IsNullOrEmpty(moduleName)
                        ? new DeepPointer(i, offsets)
                        : new DeepPointer(moduleName, i, offsets),
                    nint p => new DeepPointer(p, offsets),
                    _ => new DeepPointer((nint)Convert.ToInt64(_base), offsets)
                };

                // ---
                nint listAddr = deepPointer.Deref<nint>(Main.ProcessInstance);
                if (listAddr == 0)
                    break;

                int itemSize = Marshal.SizeOf(type);
                int count = TMemory.ReadMemory<ushort>(Main.ProcessInstance, listAddr + 0x18);
                int size = count * itemSize;

                nint listItemsAddr = listAddr;
                byte[] listBytes = TMemory.ReadMemoryBytes(Main.ProcessInstance, listItemsAddr + 0x20, size);
                if (listBytes == null || listBytes.Length == 0)
                    break;

                // race safety check
                if (listAddr != deepPointer.Deref<nint>(Main.ProcessInstance))
                    break;

                return MemoryMarshal.Cast<byte, T>(listBytes).ToArray();
            }
            while (false);
        }
        catch { }

        return [];
    }
    #endregion
    #region READ_LIST
    public List<T> ReadList<T>((nint _base, int[] offsets) offsets) where T : unmanaged
    {
        return _ReadList<T>(typeof(T), offsets._base, offsets: offsets.offsets);
    }

    public List<T> ReadList<T>(object _base, params int[] offsets) where T : unmanaged
    {
        return _ReadList<T>(typeof(T), _base, offsets: offsets);
    }

    public List<T> ReadList<T>(string moduleName, object _base, params int[] offsets) where T : unmanaged
    {
        return _ReadList<T>(typeof(T), _base, moduleName, offsets);
    }

    public List<T> ReadList<T>(Module module, object _base, params int[] offsets) where T : unmanaged
    {
        return _ReadList<T>(typeof(T), _base, module.Name, offsets);
    }

    public List<T> ReadList<T>(object _base, string moduleName = null, params int[] offsets) where T : unmanaged
    {
        return _ReadList<T>(typeof(T), _base, moduleName, offsets);
    }

    private List<T> _ReadList<T>(Type type, object _base, string moduleName = null, params int[] offsets) where T : unmanaged
    {
        try
        {
            do
            {
                DeepPointer deepPointer = _base switch
                {
                    int i => string.IsNullOrEmpty(moduleName)
                        ? new DeepPointer(i, offsets)
                        : new DeepPointer(moduleName, i, offsets),
                    nint p => new DeepPointer(p, offsets),
                    _ => new DeepPointer((nint)Convert.ToInt64(_base), offsets)
                };

                // ---
                nint listAddr = deepPointer.Deref<nint>(Main.ProcessInstance);
                if (listAddr == 0)
                    break;

                int itemSize = Marshal.SizeOf(type);
                int count = TMemory.ReadMemory<ushort>(Main.ProcessInstance, listAddr + 0x18);
                int size = count * itemSize;

                nint listItemsAddr = TMemory.ReadMemory<nint>(Main.ProcessInstance, listAddr + 0x10);
                byte[] listBytes = TMemory.ReadMemoryBytes(Main.ProcessInstance, listItemsAddr + 0x20, size);
                if (listBytes == null || listBytes.Length == 0)
                    break;

                // race safety check
                if (listAddr != deepPointer.Deref<nint>(Main.ProcessInstance))
                    break;

                // ---
                List<T> list = [];
                for (int i = 0; i < size; i += itemSize)
                {
                    var value = MemoryMarshal.Read<T>(listBytes.AsSpan(i, itemSize));
                    list.Add(value);
                }

                return list;
            }
            while (false);
        }
        catch { }

        return [];
    }
    #endregion
    #region READ_STRING
    public string ReadString(object _base, params int[] offsets)
    {
        return _ReadString(_base, offsets: offsets);
    }

    public string ReadString(int length, object _base, params int[] offsets)
    {
        return _ReadString(_base, length: length, offsets: offsets);
    }

    public string ReadString(ReadStringType readStringType, object _base, params int[] offsets)
    {
        return _ReadString(_base, offsets: offsets);
    }

    public string ReadString(int length, ReadStringType readStringType, object _base, params int[] offsets)
    {
        return _ReadString(_base, length, offsets: offsets);
    }

    public string ReadString(string moduleName, object _base, params int[] offsets)
    {
        return _ReadString(_base, moduleName: moduleName, offsets: offsets);
    }

    public string ReadString(int length, string moduleName, object _base, params int[] offsets)
    {
        return _ReadString(_base, length, moduleName: moduleName, offsets: offsets);
    }

    public string ReadString(ReadStringType readStringType, string moduleName, object _base, params int[] offsets)
    {
        return _ReadString(_base, readStringType: readStringType, moduleName: moduleName, offsets: offsets);
    }

    public string ReadString(int length, ReadStringType readStringType, string moduleName, object _base, params int[] offsets)
    {
        return _ReadString(_base, length, readStringType, moduleName, offsets);
    }

    public string ReadString(Module module, object _base, params int[] offsets)
    {
        return _ReadString(_base, moduleName: module.Name, offsets: offsets);
    }

    public string ReadString(int length, Module module, object _base, params int[] offsets)
    {
        return _ReadString(_base, length, moduleName: module.Name, offsets: offsets);
    }
    private string ReadString(ReadStringType readStringType, Module module, object _base, params int[] offsets)
    {
        return _ReadString(_base, readStringType: readStringType, moduleName: module.Name, offsets: offsets);
    }

    public string ReadString(int length, ReadStringType readStringType, Module module, object _base, params int[] offsets)
    {
        return _ReadString(_base, length, readStringType, module.Name, offsets);
    }

    public string ReadString(object _base, int length = 128, ReadStringType readStringType = ReadStringType.AutoDetect,
        string moduleName = null, params int[] offsets)
    {
        return _ReadString(_base, length, readStringType, moduleName, offsets);
    }

    private string _ReadString(object _base, int length = 128, ReadStringType readStringType = ReadStringType.AutoDetect,
        string moduleName = null, params int[] offsets)
    {
        try
        {
            DeepPointer deepPointer = _base switch
            {
                int i => string.IsNullOrEmpty(moduleName)
                    ? new DeepPointer(i, offsets)
                    : new DeepPointer(moduleName, i, offsets),
                nint p => new DeepPointer(p, offsets),
                _ => new DeepPointer((nint)Convert.ToInt64(_base), offsets)
            };

            return deepPointer.DerefString(Main.ProcessInstance, readStringType, length, null);
        }
        catch { }

        return null;
    }
    #endregion
    #region READ_BYTES
    public byte[] ReadBytes((nint _base, int[] offsets) offsets, int size)
    {
        return _ReadBytes(offsets._base, size, offsets: offsets.offsets);
    }

    public byte[] ReadBytes(object _base, int size, params int[] offsets)
    {
        return _ReadBytes(_base, size, offsets: offsets);
    }

    public byte[] ReadBytes(string moduleName, object _base, int size, params int[] offsets)
    {
        return _ReadBytes(_base, size, moduleName, offsets);
    }

    public byte[] ReadBytes(Module module, object _base, int size, params int[] offsets)
    {
        return _ReadBytes(_base, size, module.Name, offsets);
    }

    private byte[] _ReadBytes(object _base, int size, string moduleName = null, params int[] offsets)
    {
        try
        {
            DeepPointer deepPointer = _base switch
            {
                int i => string.IsNullOrEmpty(moduleName)
                    ? new DeepPointer(i, offsets)
                    : new DeepPointer(moduleName, i, offsets),
                nint p => new DeepPointer(p, offsets),
                _ => new DeepPointer((nint)Convert.ToInt64(_base), offsets)
            };

            return deepPointer.DerefBytes(Main.ProcessInstance, size);
        }
        catch { }

        return null;
    }
    #endregion

    #region WATCH
    public void Watch<T>(string name, (nint _base, int[] offsets) offsets) where T : unmanaged
    {
        _Watch<T>(name, offsets._base, offsets: offsets.offsets);
    }

    public void Watch<T>(string name, object _base, params int[] offsets) where T : unmanaged
    {
        _Watch<T>(name, _base, offsets: offsets);
    }

    public void Watch<T>(string name, string moduleName, object _base, params int[] offsets) where T : unmanaged
    {
        _Watch<T>(name, _base, moduleName, offsets);
    }

    public void Watch<T>(string name, Module module, object _base, params int[] offsets) where T : unmanaged
    {
        _Watch<T>(name, _base, module.Name, offsets);
    }

    public void Watch<T>(string name, object _base, string moduleName = null, params int[] offsets) where T : unmanaged
    {
        _Watch<T>(name, _base, moduleName, offsets);
    }

    private void _Watch<T>(string name, object _base, string moduleName = null, params int[] offsets) where T : unmanaged
    {
        try
        {
            if (!string.IsNullOrEmpty(name))
            {
                var oldWatcher = Main.MemoryWatchers.FirstOrDefault(m => m.Name == name);
                if (oldWatcher != null)
                    Main.MemoryWatchers.Remove(oldWatcher);
            }

            DeepPointer deepPointer = _base switch
            {
                int i => string.IsNullOrEmpty(moduleName)
                    ? new DeepPointer(i, offsets)
                    : new DeepPointer(moduleName, i, offsets),
                nint p => new DeepPointer(p, offsets),
                _ => new DeepPointer((nint)Convert.ToInt64(_base), offsets)
            };

            MemoryWatcher<T> memoryWatcher = new(deepPointer)
            {
                Name = name,
                Current = default
            };
            Main.MemoryWatchers.Add(memoryWatcher);
            Main.current[name] = default(T);
        }
        catch { }
    }
    #endregion
    #region WATCH_ARRAY
    public void WatchArray<T>(string name, object _base, params int[] offsets) where T : unmanaged
    {
        _WatchArray<T>(name, _base, offsets: offsets);
    }

    public void WatchArray<T>(string name, string moduleName, object _base, params int[] offsets) where T : unmanaged
    {
        _WatchArray<T>(name, _base, moduleName, offsets);
    }

    public void WatchArray<T>(string name, Module module, object _base, params int[] offsets) where T : unmanaged
    {
        _WatchArray<T>(name, _base, module.Name, offsets);
    }

    public void WatchArray<T>(string name, object _base, string moduleName = null, params int[] offsets) where T : unmanaged
    {
        _WatchArray<T>(name, _base, moduleName, offsets);
    }

    private void _WatchArray<T>(string name, object _base, string moduleName = null, params int[] offsets) where T : unmanaged
    {
        try
        {
            if (!string.IsNullOrEmpty(name))
            {
                var oldWatcher = Main.MemoryWatchers.FirstOrDefault(m => m.Name == name);
                if (oldWatcher != null)
                    Main.MemoryWatchers.Remove(oldWatcher);
            }

            DeepPointer deepPointer = _base switch
            {
                int i => string.IsNullOrEmpty(moduleName)
                    ? new DeepPointer(i, offsets)
                    : new DeepPointer(moduleName, i, offsets),
                nint p => new DeepPointer(p, offsets),
                _ => new DeepPointer((nint)Convert.ToInt64(_base), offsets)
            };

            MemoryWatcher<nint> memoryWatcher = new(deepPointer)
            {
                Name = name,
                Current = 0
            };
            Main.CountableWatchers.Add((Main.CountableStyle.Array, typeof(T), memoryWatcher, deepPointer));
            Main.current[name] = new List<T>().ToArray();
        }
        catch { }
    }
    #endregion
    #region WATCH_LIST
    public void WatchList<T>(string name, object _base, params int[] offsets) where T : unmanaged
    {
        _WatchList<T>(name, _base, offsets: offsets);
    }

    public void WatchList<T>(string name, string moduleName, object _base, params int[] offsets) where T : unmanaged
    {
        _WatchList<T>(name, _base, moduleName, offsets);
    }

    public void WatchList<T>(string name, Module module, object _base, params int[] offsets) where T : unmanaged
    {
        _WatchList<T>(name, _base, module.Name, offsets);
    }

    public void WatchList<T>(string name, object _base, string moduleName = null, params int[] offsets) where T : unmanaged
    {
        _WatchList<T>(name, _base, moduleName, offsets);
    }

    private void _WatchList<T>(string name, object _base, string moduleName = null, params int[] offsets) where T : unmanaged
    {
        try
        {
            if (!string.IsNullOrEmpty(name))
            {
                var oldWatcher = Main.MemoryWatchers.FirstOrDefault(m => m.Name == name);
                if (oldWatcher != null)
                    Main.MemoryWatchers.Remove(oldWatcher);
            }

            DeepPointer deepPointer = _base switch
            {
                int i => string.IsNullOrEmpty(moduleName)
                    ? new DeepPointer(i, offsets)
                    : new DeepPointer(moduleName, i, offsets),
                nint p => new DeepPointer(p, offsets),
                _ => new DeepPointer((nint)Convert.ToInt64(_base), offsets)
            };

            MemoryWatcher<nint> memoryWatcher = new(deepPointer)
            {
                Name = name,
                Current = 0
            };
            Main.CountableWatchers.Add((Main.CountableStyle.List, typeof(T), memoryWatcher, deepPointer));
            Main.current[name] = new List<T>();
        }
        catch { }
    }
    #endregion
    #region WATCH_STRING
    public void WatchString(string name, (nint _base, int[] offsets) offsets)
    {
        _WatchString(name, offsets._base, offsets: offsets.offsets);
    }

    public void WatchString(string name, object _base, params int[] offsets)
    {
        _WatchString(name, _base, offsets: offsets);
    }

    public void WatchString(string name, int length, object _base, params int[] offsets)
    {
        _WatchString(name, _base, length: length, offsets: offsets);
    }

    public void WatchString(string name, ReadStringType readStringType, object _base, params int[] offsets)
    {
        _WatchString(name, _base, offsets: offsets);
    }

    public void WatchString(string name, int length, ReadStringType readStringType, object _base, params int[] offsets)
    {
        _WatchString(name, _base, length, offsets: offsets);
    }

    public void WatchString(string name, string moduleName, object _base, params int[] offsets)
    {
        _WatchString(name, _base, moduleName: moduleName, offsets: offsets);
    }

    public void WatchString(string name, int length, string moduleName, object _base, params int[] offsets)
    {
        _WatchString(name, _base, length, moduleName: moduleName, offsets: offsets);
    }

    public void WatchString(string name, ReadStringType readStringType, string moduleName, object _base, params int[] offsets)
    {
        _WatchString(name, _base, readStringType: readStringType, moduleName: moduleName, offsets: offsets);
    }

    public void WatchString(string name, int length, ReadStringType readStringType, string moduleName, object _base, params int[] offsets)
    {
        _WatchString(name, _base, length, readStringType, moduleName, offsets);
    }

    public void WatchString(string name, Module module, object _base, params int[] offsets)
    {
        _WatchString(name, _base, moduleName: module.Name, offsets: offsets);
    }

    public void WatchString(string name, int length, Module module, object _base, params int[] offsets)
    {
        _WatchString(name, _base, length, moduleName: module.Name, offsets: offsets);
    }

    public void WatchString(string name, ReadStringType readStringType, Module module, object _base, params int[] offsets)
    {
        _WatchString(name, _base, readStringType: readStringType, moduleName: module.Name, offsets: offsets);
    }

    public void WatchString(string name, int length, ReadStringType readStringType, Module module, object _base, params int[] offsets)
    {
        _WatchString(name, _base, length, readStringType, module.Name, offsets);
    }

    public void WatchString(string name, object _base, int length = 128, ReadStringType readStringType = ReadStringType.AutoDetect,
        string moduleName = null, params int[] offsets)
    {
        _WatchString(name, _base, length, readStringType, moduleName, offsets);
    }

    private void _WatchString(string name, object _base, int length = 128, ReadStringType readStringType = ReadStringType.AutoDetect,
        string moduleName = null, params int[] offsets)
    {
        try
        {
            if (!string.IsNullOrEmpty(name))
            {
                var oldWatcher = Main.MemoryWatchers.FirstOrDefault(m => m.Name == name);
                if (oldWatcher != null)
                    Main.MemoryWatchers.Remove(oldWatcher);
            }

            DeepPointer deepPointer = _base switch
            {
                int i => string.IsNullOrEmpty(moduleName)
                    ? new DeepPointer(i, offsets)
                    : new DeepPointer(moduleName, i, offsets),
                nint p => new DeepPointer(p, offsets),
                _ => new DeepPointer((nint)Convert.ToInt64(_base), offsets)
            };

            StringWatcher stringWatcher = new(deepPointer, readStringType, length)
            {
                Name = name,
                Current = null
            };
            Main.StringWatchers.Add(stringWatcher);
            Main.current[name] = null;
        }
        catch { }
    }
    #endregion
    #region WATCH_UNITY_STRING
    public void WatchUnityString(string name, (nint _base, int[] offsets) offsets)
    {
        _WatchUnityString(name, offsets._base, offsets: offsets.offsets);
    }

    public void WatchUnityString(string name, object _base, params int[] offsets)
    {
        _WatchUnityString(name, _base, offsets: offsets);
    }

    public void WatchUnityString(string name, int length, object _base, params int[] offsets)
    {
        _WatchUnityString(name, _base, length: length, offsets: offsets);
    }

    public void WatchUnityString(string name, ReadStringType readStringType, object _base, params int[] offsets)
    {
        _WatchUnityString(name, _base, offsets: offsets);
    }

    public void WatchUnityString(string name, int length, ReadStringType readStringType, object _base, params int[] offsets)
    {
        _WatchUnityString(name, _base, length, offsets: offsets);
    }

    public void WatchUnityString(string name, string moduleName, object _base, params int[] offsets)
    {
        _WatchUnityString(name, _base, moduleName: moduleName, offsets: offsets);
    }

    public void WatchUnityString(string name, int length, string moduleName, object _base, params int[] offsets)
    {
        _WatchUnityString(name, _base, length, moduleName: moduleName, offsets: offsets);
    }

    public void WatchUnityString(string name, ReadStringType readStringType, string moduleName, object _base, params int[] offsets)
    {
        _WatchUnityString(name, _base, readStringType: readStringType, moduleName: moduleName, offsets: offsets);
    }

    public void WatchUnityString(string name, int length, ReadStringType readStringType, string moduleName, object _base, params int[] offsets)
    {
        _WatchUnityString(name, _base, length, readStringType, moduleName, offsets);
    }

    public void WatchUnityString(string name, Module module, object _base, params int[] offsets)
    {
        _WatchUnityString(name, _base, moduleName: module.Name, offsets: offsets);
    }

    public void WatchUnityString(string name, int length, Module module, object _base, params int[] offsets)
    {
        _WatchUnityString(name, _base, length, moduleName: module.Name, offsets: offsets);
    }

    public void WatchUnityString(string name, ReadStringType readStringType, Module module, object _base, params int[] offsets)
    {
        _WatchUnityString(name, _base, readStringType: readStringType, moduleName: module.Name, offsets: offsets);
    }

    public void WatchUnityString(string name, int length, ReadStringType readStringType, Module module, object _base, params int[] offsets)
    {
        _WatchUnityString(name, _base, length, readStringType, module.Name, offsets);
    }

    public void WatchUnityString(string name, object _base, int length = 128, ReadStringType readStringType = ReadStringType.AutoDetect,
        string moduleName = null, params int[] offsets)
    {
        _WatchUnityString(name, _base, length, readStringType, moduleName, offsets);
    }

    private void _WatchUnityString(string name, object _base, int length = 128, ReadStringType readStringType = ReadStringType.UTF16,
        string moduleName = null, params int[] offsets)
    {
        try
        {
            if (!string.IsNullOrEmpty(name))
            {
                var oldWatcher = Main.MemoryWatchers.FirstOrDefault(m => m.Name == name);
                if (oldWatcher != null)
                    Main.MemoryWatchers.Remove(oldWatcher);
            }

            List<int> _offsets = [.. offsets];
            _offsets.Add(0x14);
            offsets = [.. _offsets];

            DeepPointer deepPointer = _base switch
            {
                int i => string.IsNullOrEmpty(moduleName)
                    ? new DeepPointer(i, offsets)
                    : new DeepPointer(moduleName, i, offsets),
                nint p => new DeepPointer(p, offsets),
                _ => new DeepPointer((nint)Convert.ToInt64(_base), offsets)
            };

            StringWatcher stringWatcher = new(deepPointer, 128)
            {
                Name = name,
                Current = null
            };
            Main.StringWatchers.Add(stringWatcher);
            Main.current[name] = null;
        }
        catch { }
    }
    #endregion

    #region UTILITIES
    public bool CheckFlag(string watcherName)
    {
        try
        {
            do
            {
                ulong curr = Convert.ToUInt64(Main.current[watcherName]);
                ulong ol = Convert.ToUInt64(Main.old[watcherName]);
                return curr != ol && curr != 0;
            }
            while (false);
        }
        catch { }

        return false;
    }
    #endregion
}
