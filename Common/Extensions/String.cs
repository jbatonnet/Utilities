using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace System
{
    public static class StringExtensions
    {
        public static int IndexOfAny(this string me, params char[] values)
        {
            return me.IndexOfAny(values);
        }

        public static string[] SplitWords(this string me)
        {
            return me.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        }
        public static string[] SplitLines(this string me)
        {
            return me.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string Before(this string me, string value)
        {
            int index = me.IndexOf(value);
            return index == -1 ? me : me.Substring(0, index);
        }
        public static string After(this string me, string value)
        {
            int index = me.IndexOf(value);
            return index == -1 ? me : me.Substring(index + value.Length);
        }
        public static string Between(this string me, string begin, string end)
        {
            if (me.Contains(begin))
                me = me.Substring(me.IndexOf(begin) + begin.Length);
            if (me.Contains(end))
                me = me.Remove(me.IndexOf(end));
            return me;
        }

        public static bool ContainsAny(this string me, params string[] values)
        {
            foreach (string value in values)
                if (me.Contains(value))
                    return true;

            return false;
        }
    }
}