using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.IO
{
    public static class StreamExtension
    {
        public static byte[] ReadBytes(this Stream me)
        {
            MemoryStream memoryStream = new MemoryStream((int)me.Length);
            me.CopyTo(memoryStream);
            return memoryStream.GetBuffer();
        }
    }
}
