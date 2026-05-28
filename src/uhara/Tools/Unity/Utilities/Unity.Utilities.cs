using System;
using System.Threading;

public partial class Tools
{
    public partial class Unity
    {
        public partial class Utilities
        {
            private static readonly string ToolUniqueID = "LMYpsRecShieLHhD";

            private readonly SceneManager sceneManager = null;

            internal static bool LegacyVersion = false;

            #region PUBLIC_API
            public string[] GetAllSceneNames()
            {
                try
                {
                    do
                    {
                        return sceneManager?.GetAllSceneNames();
                    }
                    while (false);
                }
                catch { }

                return null;
            }

            public string GetActiveSceneName()
            {
                try
                {
                    do
                    {
                        return sceneManager?.GetCurrentSceneName();
                    }
                    while (false);
                }
                catch { }

                return null;
            }

            public string GetCurrentSceneName()
            {
                try
                {
                    do
                    {
                        return sceneManager?.GetCurrentSceneName();
                    }
                    while (false);
                }
                catch { }

                return null;
            }

            public string GetActiveSceneName2()
            {
                try
                {
                    do
                    {
                        return sceneManager?.GetCurrentSceneName2();
                    }
                    while (false);
                }
                catch { }

                return null;
            }

            public string GetCurrentSceneName2()
            {
                try
                {
                    do
                    {
                        return sceneManager?.GetCurrentSceneName2();
                    }
                    while (false);
                }
                catch { }

                return null;
            }

            public string GetLoadingSceneName()
            {
                try
                {
                    do
                    {
                        return sceneManager?.GetLoadingSceneName();
                    }
                    while (false);
                }
                catch { }

                return null;
            }
            #endregion

            #region CONSTRUCTOR
            public Utilities()
            {
                try
                {
                    do
                    {
                        if (!Main.ReloadProcess())
                            throw new Exception();
                        Thread.Sleep(100);
                    }
                    while (Main.ProcessInstance.MainWindowHandle == IntPtr.Zero);

                    bool success = false;
                    while (!success)
                    {
                        do
                        {
                            if (!Main.ReloadProcess())
                                throw new Exception();
                            if (Main.ProcessInstance.GetModule("mono-2.0-bdwgc.dll").BaseAddress != IntPtr.Zero ||
                                Main.ProcessInstance.GetModule("GameAssembly.dll").BaseAddress != IntPtr.Zero)
                            {
                                if (Main.ProcessInstance.GetModule("UnityPlayer.dll").BaseAddress == IntPtr.Zero)
                                    break;
                                byte[] modBytes = Main.ProcessInstance.GetModuleBytes("UnityPlayer.dll");
                                if (modBytes == null || modBytes.Length == 0)
                                    break;
                            }
                            else if (Main.ProcessInstance.GetModule("mono.dll").BaseAddress == IntPtr.Zero)
                                break;
                            else
                                LegacyVersion = true;

                            success = true;
                        }
                        while (false);
                        Thread.Sleep(300);
                    }

                    if (!Main.ReloadProcess())
                        throw new Exception();
                    MemoryManager.ClearMemory(ToolUniqueID);

                    // ---
                    sceneManager = new SceneManager();
                }
                catch { }
            }
            #endregion
        }
    }
}
