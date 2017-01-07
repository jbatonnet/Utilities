using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Collections.Generic
{
    public static class ListExtensions
    {
        public static void AddRange<T>(this List<T> me, params T[] items)
        {
            me.AddRange(items);
        }
        public static int IndexOf<T>(this List<T> me, Predicate<T> predicate)
        {
            int index = me.TakeWhile(i => !predicate(i)).Count();

            if (me.Count == index)
                return -1;
            return index;
        }
        public static int LastIndexOf<T>(this List<T> me, Predicate<T> predicate)
        {
            int index = me.Count - 1 - me.Reverse<T>().TakeWhile(i => !predicate(i)).Count();

            if (me.Count == index)
                return -1;
            return index;
        }
        public static void Replace<T>(this List<T> me, T source, T destination)
        {
            int i = me.IndexOf(source);
            me[i] = destination;
        }
    }
}