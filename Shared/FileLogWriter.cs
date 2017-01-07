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
    public class FileLogWriter : TextWriter
    {
        public override Encoding Encoding
        {
            get
            {
                return Encoding.Default;
            }
        }
        public Func<string, string> Formatter { get; set; }
        
        private FileInfo fileInfo;
        private StringBuilder lineBuilder = new StringBuilder();

        public FileLogWriter(string path) : this(new FileInfo(path)) { }
        public FileLogWriter(FileInfo fileInfo)
        {
            Formatter = Format;

            this.fileInfo = fileInfo;
        }

        public override void Write(char value)
        {
            if (value == '\r' || value == '\n')
            {
                if (lineBuilder.Length == 0)
                    return;

                WriteLine(lineBuilder.ToString());
                lineBuilder.Clear();
            }
            else
                lineBuilder.Append(value);
        }
        public override void WriteLine(string value)
        {
            using (StreamWriter writer = fileInfo.AppendText())
                writer.WriteLine(Format(value));
        }

        public virtual string Format(string value)
        {
            return string.Format("[{0}] {1}", DateTime.Now.ToLongTimeString(), value);
        }
    }
}