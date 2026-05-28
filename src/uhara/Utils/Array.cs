using System;
using System.Collections.Generic;

internal class TArray
{
    internal static byte[] DecodeBlock(byte[] asmBlock)
    {
        List<byte> decoded = [];
        for (int i = 0; i < asmBlock.Length; i++)
            if (i % 2 == 0)
                decoded.Add(asmBlock[i]);

        return [.. decoded];
    }

    internal static void Insert(byte[] destination, byte[] toInsert, int position)
    {
        Array.Copy(toInsert, 0, destination, position, toInsert.Length);
    }
}
