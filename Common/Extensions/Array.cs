using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    public static class ArrayExtension
    {
        public static int IndexOf<T>(this T[] me, T item)
        {
            for (int i = 0; i < me.Length; i++)
                if (me[i]?.Equals(item) == true)
                    return i;

            return -1;
        }
    }
}
