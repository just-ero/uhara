using System;

public partial class Tools
{
    public partial class Unity
    {
        public partial class Default
        {
            public partial class GameObject
            {
                #region PUBLIC_API
                public Converter.GameObjectResolvable InstanceToGameObjectResolvable(nint instanceAddress, bool isInsideAPointer)
                {
                    return converter.InstanceToGameObjectResolvable((ulong)instanceAddress, isInsideAPointer);
                }

                public Converter.GameObjectResolvable InstanceToGameObjectResolvable(ulong instanceAddress, bool isInsideAPointer)
                {
                    return converter.InstanceToGameObjectResolvable(instanceAddress, isInsideAPointer);
                }

                public Converter.GameObjectResolvable GameObjectToGameObjectResolvable(nint instanceAddress, bool isInsideAPointer)
                {
                    return converter.GameObjectToGameObjectResolvable((ulong)instanceAddress, isInsideAPointer);
                }

                public Converter.GameObjectResolvable GameObjectToGameObjectResolvable(ulong instanceAddress, bool isInsideAPointer)
                {
                    return converter.GameObjectToGameObjectResolvable(instanceAddress, isInsideAPointer);
                }
                #endregion

                #region VARIABLES
                internal static string ToolUniqueID = "BhuPdeQvHKvIDKiP";
                internal static Converter converter;
                #endregion

                #region CONSTRUCTOR
                public GameObject()
                {
                    try
                    {
                        try
                        {
                            while (true)
                            {
                                if (!Main.ReloadProcess())
                                    throw new Exception();

                                if (Main.ProcessInstance.MainWindowHandle != IntPtr.Zero)
                                    break;

                                throw new Exception();
                            }

                            bool success = false;
                            while (!success)
                            {
                                do
                                {
                                    if (!Main.ReloadProcess())
                                        throw new Exception();
                                    try
                                    {
                                        if (Main.ProcessInstance == null)
                                            break;

                                        if (Main.ProcessInstance.GetModule("mono-2.0-bdwgc.dll").BaseAddress != IntPtr.Zero)
                                        {
                                            if (Main.ProcessInstance.GetModule("UnityPlayer.dll").BaseAddress == IntPtr.Zero)
                                                break;
                                            byte[] modBytes = Main.ProcessInstance.GetModuleBytes("UnityPlayer.dll");
                                            if (modBytes == null || modBytes.Length == 0)
                                                break;
                                        }
                                        else
                                            break;

                                        if (Main.ProcessInstance.GetModule("kernel32.dll").BaseAddress == IntPtr.Zero)
                                            break;
                                    }
                                    catch { }

                                    success = true;
                                }
                                while (false);
                                if (!success)
                                    throw new Exception();
                            }
                        }
                        catch
                        {
                            return;
                        }

                        MemoryManager.ClearMemory(ToolUniqueID);
                    }
                    catch
                    {
                        return;
                    }

                    // ---
                    converter = new Converter();
                    if (!converter.Initiate())
                        throw new Exception();
                }
                #endregion
            }
        }
    }
}
