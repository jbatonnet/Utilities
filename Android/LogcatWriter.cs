using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Android.Util;

namespace Android.Utilities
{
    public class LogcatWriter : TextWriter
    {
        public override Encoding Encoding
        {
            get
            {
                return Encoding.Default;
            }
        }

        private string tag;
        private LogPriority priority;
        private StringBuilder lineBuilder = new StringBuilder();

        public LogcatWriter(string tag, LogPriority priority)
        {
            this.tag = tag;
            this.priority = priority;
        }

        public override void Write(char value)
        {
            if (value == '\r' || value == '\n')
            {
                if (lineBuilder.Length == 0)
                    return;

                Android.Util.Log.WriteLine(priority, tag, lineBuilder.ToString());
                lineBuilder.Clear();
            }
            else
                lineBuilder.Append(value);
        }
        public override void WriteLine(string value)
        {
            Android.Util.Log.WriteLine(priority, tag, value);
        }
    }
}