using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    public static class ByteExtension
    {
        public static string ToHexString(this byte[] byteArray)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (byte b in byteArray)
                stringBuilder.Append(b.ToString("X2"));
            return stringBuilder.ToString().ToLower();
        }
        public static string ToHexString(this byte[] byteArray, char separator)
        {
            if (byteArray.Length == 0)
                return "";

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(byteArray[0].ToString("X2"));
            for (int i = 1; i < byteArray.Length; i++)
            {
                stringBuilder.Append(separator);
                stringBuilder.Append(byteArray[i].ToString("X2"));
            }
            return stringBuilder.ToString().ToLower();
        }
    }
}
