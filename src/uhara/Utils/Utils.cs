using System;
using System.Collections.Generic;
using System.Text;

internal class TUtils
{
    internal static string MultibyteToString(byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return string.Empty;

        StringBuilder sb = new();
        for (int i = 0; i < bytes.Length && bytes[i] != 0; i++)
        {
            sb.Append((char)bytes[i]);
        }

        return sb.ToString();
    }

    internal static string MultibyteToString2(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return string.Empty;

        StringBuilder sb = new();
        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] is < 32 or > 126)
                break;
            else
                sb.Append((char)bytes[i]);
        }

        //sb.Append(0);
        return sb.ToString();
    }

    internal static string GenerateRandomString(int length)
    {
        string text = "";
        while (text.Length <= length)
            text += Guid.NewGuid().ToString("N");

        text = text[..length];
        return text;
    }

    internal static byte[] StringToMultibyte(string text)
    {
        List<byte> array = [];
        for (int i = 0; i < text.Length; i++)
        {
            array.Add(Convert.ToByte(text[i]));
        }

        array.Add(0);
        return [.. array];
    }

    internal static void Print(string message)
    {
        if (Main.DebugMode)
            TImports.OutputDebugString("[UHARA] " + message);
    }
}
