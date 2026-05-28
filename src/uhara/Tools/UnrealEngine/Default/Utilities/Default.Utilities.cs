using System;

public partial class Tools
{
    public partial class UnrealEngine
    {
        public partial class Default
        {
            public partial class Utilities
            {
                internal static string DebugClass = "Utilities";
                internal static string ToolUniqueID = "UCyEljVfhjUoJhDU";

                private readonly DataRetriever dataRetriever;
                private readonly TextReader textReader;
                private readonly FpsLocker fpsLocker;

                #region PUBLIC_API
                public void ExpandScanUtilitySignatures(string name, string signature)
                {
                    try
                    {
                        if (ScanUtility.ExpandSignatures.ContainsKey(name))
                            ScanUtility.ExpandSignatures[name].Add(signature);
                        else
                            ScanUtility.ExpandSignatures[name] = [signature];
                    }
                    catch { }
                }

                public void SetFpsLimit(double fps)
                {
                    try
                    {
                        fpsLocker.SetFpsLimit(fps);
                    }
                    catch { }
                }

                public string FNameToStringLegacy(object fName)
                {
                    try
                    {
                        return textReader.FNameToStringLegacy(fName);
                    }
                    catch { }

                    return null;
                }

                public string FNameToShortStringLegacy(object fName)
                {
                    try
                    {
                        return textReader.FNameToShortStringLegacy(fName);
                    }
                    catch { }

                    return null;
                }

                public string FNameToShortStringLegacy2(object fName)
                {
                    try
                    {
                        return textReader.FNameToShortStringLegacy2(fName);
                    }
                    catch { }

                    return null;
                }

                public string FNameToString(object fName)
                {
                    try
                    {
                        return textReader.FNameToString(fName);
                    }
                    catch { }

                    return null;
                }

                public string FNameToShortString(object fName)
                {
                    try
                    {
                        return textReader.FNameToShortString(fName);
                    }
                    catch { }

                    return null;
                }

                public string FNameToShortString2(object fName)
                {
                    try
                    {
                        return textReader.FNameToShortString2(fName);
                    }
                    catch { }

                    return null;
                }

                public nint GEngine
                {
                    get
                    {
                        if (field != 0)
                            return field;
                        else
                        {
                            field = dataRetriever.FindData("GEngine");
                            return field;
                        }
                    }

                    set;
                } = 0;

                public nint GWorld
                {
                    get
                    {
                        if (field != 0)
                            return field;
                        else
                        {
                            field = dataRetriever.FindData("GWorld");
                            return field;
                        }
                    }

                    set;
                } = 0;

                public nint FNamePool
                {
                    get
                    {
                        if (field != 0)
                            return field;
                        else
                        {
                            field = dataRetriever.FindData("FNames");
                            return field;
                        }
                    }

                    set;
                } = 0;

                public nint FNames
                {
                    get
                    {
                        if (field != 0)
                            return field;
                        else
                        {
                            field = dataRetriever.FindData("FNames");
                            return field;
                        }
                    }

                    set;
                } = 0;

                public nint GSync
                {
                    get
                    {
                        if (field != 0)
                            return field;
                        else
                        {
                            field = dataRetriever.FindData("GSync");
                            return field;
                        }
                    }

                    set;
                } = 0;

                public nint FindData(string dataName)
                {
                    try
                    {
                        return dataRetriever.FindData(dataName);
                    }
                    catch { }

                    return 0;
                }
                #endregion

                public Utilities()
                {
                    if (!Main.ReloadProcess())
                        throw new Exception();
                    ulong modBase = TProcess.GetModuleBase(Main.ProcessInstance);
                    if (modBase == 0)
                        throw new Exception();

                    // ---
                    MemoryManager.ClearMemory(ToolUniqueID);

                    // ---
                    dataRetriever = new DataRetriever();
                    textReader = new TextReader();
                    fpsLocker = new FpsLocker();
                }
            }
        }
    }
}
