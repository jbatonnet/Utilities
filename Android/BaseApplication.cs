using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

using Android.App;
using Android.Content;
using Android.Runtime;

using LogPriority = Android.Util.LogPriority;

namespace Android.Utilities
{
    public class BaseApplication : Application
    {
        public static BaseApplication Instance { get; private set; }

        public virtual string Name { get; }
        public virtual DbConnection Database
        {
            get
            {
                if (database == null)
                {
                    database = new AndroidDatabaseConnection(this, Name + ".db");
                    database.VersionUpgraded += OnDatabaseVersionUpgrade;
                    database.Open();
                }

                return database;
            }
        }

        private AndroidDatabaseConnection database;

        protected BaseApplication(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
            Instance = this;

            // Initialize logging
#if DEBUG
            Log.TraceStream = new LogcatWriter(Name, LogPriority.Verbose);
            Log.DebugStream = new LogcatWriter(Name, LogPriority.Debug);
#endif
            Log.InfoStream = new LogcatWriter(Name, LogPriority.Info);
            Log.WarningStream = new LogcatWriter(Name, LogPriority.Warn);
            Log.ErrorStream = new LogcatWriter(Name, LogPriority.Error);
        }

        protected virtual void OnDatabaseVersionUpgrade(AndroidDatabaseConnection connection, int oldVersion, int newVersion) { }

        public void StartService(Type type)
        {
            (this as ContextWrapper).StartService(type);
        }
    }
}
