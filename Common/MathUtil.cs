using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System
{
    public static class MathUtil
    {
        public static int Clamp(int value, int minimum, int maximum)
        {
            return value < minimum ? minimum : value > maximum ? maximum : value;
        }
        public static float Clamp(float value, float minimum, float maximum)
        {
            return value < minimum ? minimum : value > maximum ? maximum : value;
        }
        public static double Clamp(double value, double minimum, double maximum)
        {
            return value < minimum ? minimum : value > maximum ? maximum : value;
        }

        public static float Map(float value, float sourceFrom, float sourceTo, float destinationFrom, float destinationTo)
        {
            return destinationFrom + (destinationTo - destinationFrom) * ((value - sourceFrom) / (sourceTo - sourceFrom));
        }
        public static double Map(double value, double sourceFrom, double sourceTo, double destinationFrom, double destinationTo)
        {
            return destinationFrom + (destinationTo - destinationFrom) * ((value - sourceFrom) / (sourceTo - sourceFrom));
        }
    }
}