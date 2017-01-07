using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    public static class RandomExtension
    {
        public static byte NextByte(this Random random)
        {
            return (byte)random.Next(256);
        }
        public static float NextSingle(this Random random)
        {
            return (float)random.NextDouble();
        }
    }
}