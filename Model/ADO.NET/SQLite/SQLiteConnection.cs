#if MODEL_ADONET_SQLITE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Reflection;
using System.Text;

public class SQLiteConnection : AdoNetConnection
{
    public SQLiteConnection() : base(new System.Data.SQLite.SQLiteConnection("Data Source=:memory:;Version=3")) { }
    public SQLiteConnection(FileInfo file) : base(new System.Data.SQLite.SQLiteConnection("Data Source=" + file.FullName + ";Version=3")) { }
}

#endif