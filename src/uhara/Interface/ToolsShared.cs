internal class ToolsShared
{
    internal class ToolData
    {
        internal class UnrealEngine
        {
            public static ulong F_StaticConstructObject_Internal = 0;
            public static ulong F_UObjectBeginDestroy = 0;
            public static ulong F_UObjectProcessEvent = 0;
            public static ulong D_FNamePoolAddress = 0;
        }
    }

    internal class ToolNames
    {
        internal class Unity
        {
            internal static readonly string[] Data = ["unity", "unityengine", "unity3d"];

            internal class Default
            {
                internal static readonly string[] Data = ["default"];

                internal class GameObject
                {
                    internal static readonly string[] Data = ["gameobject"];
                }
            }

            internal class Utils
            {
                internal static readonly string[] Data = ["utils", "utilities"];
            }

            internal class DotNet
            {
                internal static readonly string[] Data = ["dotnet", "cs", "csharp", "mono"];

                internal class JitSave
                {
                    internal static readonly string[] Data = ["jitsave"];
                }

                internal class Instance
                {
                    internal static readonly string[] Data = ["instance"];
                }
            }

            internal class Il2Cpp
            {
                internal static readonly string[] Data = ["il2cpp", "cpp"];

                internal class JitSave
                {
                    internal static readonly string[] Data = ["jitsave"];
                }

                internal class Instance
                {
                    internal static readonly string[] Data = ["instance"];
                }
            }
        }

        internal class UnrealEngine
        {
            internal static readonly string[] Data = ["unrealengine"];

            internal class Default
            {
                internal static readonly string[] Data = ["default"];

                internal class Events
                {
                    internal static readonly string[] Data = ["events"];
                }

                internal class Utilities
                {
                    internal static readonly string[] Data = ["utils", "utilities"];
                }

                internal class CutsceneManager
                {
                    internal static readonly string[] Data = ["cutscenemanager"];
                }
            }
        }
    }
}
