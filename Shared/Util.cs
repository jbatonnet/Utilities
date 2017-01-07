using System;
using System.Collections.Generic;
using System.Text;

public static class Util
{
    public static int Hash(params object[] args)
    {
        int hash = 0;

        foreach (object arg in args)
        {
            hash = hash << 8 | ~hash >> 24;

            if (arg != null)
                hash ^= arg.GetHashCode();
        }

        return hash;
    }
}
