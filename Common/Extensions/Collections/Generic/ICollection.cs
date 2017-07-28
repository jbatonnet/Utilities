using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Collections.Generic
{
    public static class ICollectionExtensions
    {
        public static void AddRange<T>(this ICollection<T> me, IEnumerable<T> items)
        {
            foreach (T item in items)
                me.Add(item);
        }
    }
}