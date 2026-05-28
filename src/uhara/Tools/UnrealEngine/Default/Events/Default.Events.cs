using System;
using System.Collections.Generic;

public partial class Tools
{
    public partial class UnrealEngine
    {
        public partial class Default
        {
            public partial class Events
            {
                internal static string DebugClass = "Instance";
                internal static string ToolUniqueID = "OlDIgZzLoZjiyHwu";

                private readonly FunctionCall functionCall = new();
                private readonly InstanceCreation instanceCreation = new();

                #region PUBLIC_API
                public nint GetLastFunctionCallInstanceDestroyFlagPointer()
                {
                    try
                    {
                        return functionCall.GetLastDestroyInstanceFlagPointer();
                    }
                    catch { }

                    return 0;
                }

                public nint GetLastInstanceCreationInstanceDestroyFlagPointer()
                {
                    try
                    {
                        return instanceCreation.GetLastDestroyInstanceFlagPointer();
                    }
                    catch { }

                    return 0;
                }

                public nint InstancePtr(string className, string objectName)
                {
                    try
                    {
                        return instanceCreation.AddArgument(InstanceCreation.ArgTypes.Instance, className, objectName, 1);
                    }
                    catch { }

                    return 0;
                }

                public nint[] InstancePtr(string className, string objectName, short instances)
                {
                    try
                    {
                        do
                        {
                            nint basePtr = instanceCreation.AddArgument(InstanceCreation.ArgTypes.Instance, className, objectName, instances);
                            if (basePtr == 0)
                                break;

                            List<nint> result = [];
                            for (int i = 0; i < instances; i++)
                                result.Add(basePtr + (0x8 * i));
                            return [.. result];
                        }
                        while (false);
                    }
                    catch { }

                    return [];
                }

                public nint InstanceFlag(string className, string objectName)
                {
                    try
                    {
                        return instanceCreation.AddArgument(InstanceCreation.ArgTypes.Flag, className, objectName, 1);
                    }
                    catch { }

                    return 0;
                }

                public nint FunctionFlag(string className, string objectName, string functionName)
                {
                    try
                    {
                        return functionCall.AddArgument(FunctionCall.ArgTypes.Flag, className, objectName, functionName, 1);
                    }
                    catch { }

                    return 0;
                }

                public void FunctionFlag(string watcherName, string className, string objectName, string functionName)
                {
                    try
                    {
                        new PtrResolver().Watch<ulong>(watcherName, functionCall.AddArgument(FunctionCall.ArgTypes.Flag, className, objectName, functionName, 1));
                    }
                    catch { }
                }

                public nint FunctionParentPtr(string className, string objectName, string functionName)
                {
                    try
                    {
                        return functionCall.AddArgument(FunctionCall.ArgTypes.Instance, className, objectName, functionName, 1);
                    }
                    catch { }

                    return 0;
                }

                public void FunctionParentPtr<T>(string watcherName, string className, string objectName, string functionName) where T : unmanaged
                {
                    try
                    {
                        new PtrResolver().Watch<T>(watcherName, functionCall.AddArgument(FunctionCall.ArgTypes.Instance, className, objectName, functionName, 1));
                    }
                    catch { }
                }
                #endregion

                public Events()
                {
                    if (!Main.ReloadProcess())
                        throw new Exception();
                    MemoryManager.ClearMemory(ToolUniqueID);
                }
            }
        }
    }
}
