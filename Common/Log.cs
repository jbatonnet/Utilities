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
    public enum LogVerbosity
    {
        Trace,
        Debug,
        Info,
        Warning,
        Error
    }

    public static class Log
    {
        public static TextWriter TraceStream { get; set; }
        public static TextWriter DebugStream { get; set; }
        public static TextWriter InfoStream { get; set; }
        public static TextWriter WarningStream { get; set; }
        public static TextWriter ErrorStream { get; set; }

        public static LogVerbosity Verbosity { get; set; } =
#if DEBUG
            LogVerbosity.Debug;
#else
            LogVerbosity.Warning;
#endif

        private static object mutex = new object();

        static Log()
        {
            TraceStream = DebugStream = InfoStream = WarningStream = Console.Out;
            ErrorStream = Console.Error;
        }

        public static void Trace(string format, params object[] args)
        {
            if (Verbosity > LogVerbosity.Trace)
                return;

            lock (TraceStream)
            {
                if (args == null || args.Length == 0)
                    TraceStream.WriteLine(format);
                else
                    TraceStream.WriteLine(format, args);
            }
        }
        public static void Debug(string format, params object[] args)
        {
            if (Verbosity > LogVerbosity.Debug)
                return;

            lock (DebugStream)
            {
                if (args == null || args.Length == 0)
                    DebugStream.WriteLine(format);
                else
                    DebugStream.WriteLine(format, args);
            }
        }
        public static void Info(string format, params object[] args)
        {
            if (Verbosity > LogVerbosity.Info)
                return;

            lock (InfoStream)
            {
                if (args == null || args.Length == 0)
                    InfoStream.WriteLine(format);
                else
                    InfoStream.WriteLine(format, args);
            }
        }
        public static void Warning(string format, params object[] args)
        {
            if (Verbosity > LogVerbosity.Warning)
                return;

            lock (WarningStream)
            {
                if (args == null || args.Length == 0)
                    WarningStream.WriteLine(format);
                else
                    WarningStream.WriteLine(format, args);
            }
        }
        public static void Error(string format, params object[] args)
        {
            if (Verbosity > LogVerbosity.Error)
                return;

            lock (ErrorStream)
            {
                if (args == null || args.Length == 0)
                    ErrorStream.WriteLine(format);
                else
                    ErrorStream.WriteLine(format, args);
            }
        }
    }
}