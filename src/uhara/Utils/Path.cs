using System.IO;

internal class TPath
{
    internal static string FindFile(string dir, string fileName)
    {
        if (!Directory.Exists(dir))
        {
            return "";
        }

        string[] allFiles = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
        foreach (string file in allFiles)
        {
            if (file.EndsWith(fileName))
                return file;
        }

        return "";
    }
}
