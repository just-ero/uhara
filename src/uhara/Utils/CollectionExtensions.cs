using System;
using System.Collections.Generic;
using System.Linq;

internal static class CollectionExtensions
{
    extension<T>(IEnumerable<T> source)
    {
        public IEnumerable<T> Stride(int stride)
        {
            return source.Where((_, i) => i % stride == 0);
        }
    }

    extension<T>(Array)
    {
        public static void Insert(T[] source, T[] destination, int position)
        {
            Array.Copy(source, 0, destination, position, destination.Length);
        }
    }
}
