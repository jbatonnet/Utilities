using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;

using Android.Content;
using Android.Database;
using Android.Database.Sqlite;
using Android.OS;

namespace Android.Utilities
{
    public delegate void AndroidDatabaseUpgradeCallback(AndroidDatabaseConnection connection, int oldVersion, int newVersion);

    public class AndroidDatabaseReader : DbDataReader
    {
        public override object this[string name]
        {
            get
            {
                return GetValue(cursor.GetColumnIndexOrThrow(name));
            }
        }
        public override object this[int ordinal]
        {
            get
            {
                return GetValue(ordinal);
            }
        }

        public override int Depth
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public override int FieldCount
        {
            get
            {
                return cursor.ColumnCount;
            }
        }
        public override bool HasRows
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public override bool IsClosed
        {
            get
            {
                return cursor.IsClosed;
            }
        }
        public override int RecordsAffected
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        private ICursor cursor;

        public AndroidDatabaseReader(ICursor cursor)
        {
            this.cursor = cursor;
        }

        public override bool GetBoolean(int ordinal)
        {
            return cursor.GetInt(ordinal) != 0;
        }
        public override byte GetByte(int ordinal)
        {
            return (byte)cursor.GetInt(ordinal);
        }
        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            byte[] blob = cursor.GetBlob(ordinal);

            length = (int)Math.Min(Math.Min(length, blob.Length - dataOffset), buffer.Length - bufferOffset);
            Array.Copy(blob, dataOffset, buffer, bufferOffset, length);

            return length;
        }
        public override char GetChar(int ordinal)
        {
            return (char)cursor.GetInt(ordinal);
        }
        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            string text = cursor.GetString(ordinal);

            length = (int)Math.Min(Math.Min(length, text.Length - dataOffset), buffer.Length - bufferOffset);
            for (int i = 0; i < length; i++)
                buffer[bufferOffset + i] = text[(int)dataOffset + i];

            return length;
        }
        public override string GetDataTypeName(int ordinal)
        {
            return cursor.GetType(ordinal).ToString();
        }
        public override DateTime GetDateTime(int ordinal)
        {
            switch (cursor.GetType(ordinal))
            {
                case FieldType.Integer: return new DateTime(cursor.GetInt(ordinal));
                case FieldType.String: return DateTime.Parse(cursor.GetString(ordinal));
            }

            throw new FormatException("The value being read is not a valid DateTime");
        }
        public override decimal GetDecimal(int ordinal)
        {
            return (decimal)cursor.GetDouble(ordinal);
        }
        public override double GetDouble(int ordinal)
        {
            return cursor.GetDouble(ordinal);
        }
        public override IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }
        public override Type GetFieldType(int ordinal)
        {
            switch (cursor.GetType(ordinal))
            {
                case FieldType.Null: return null;
                case FieldType.Float: return typeof(float);
                case FieldType.Integer: return typeof(int);
                case FieldType.String: return typeof(string);
                case FieldType.Blob: return typeof(byte[]);
            }

            return null;
        }
        public override float GetFloat(int ordinal)
        {
            return cursor.GetFloat(ordinal);
        }
        public override Guid GetGuid(int ordinal)
        {
            return Guid.Parse(cursor.GetString(ordinal));
        }
        public override short GetInt16(int ordinal)
        {
            return (short)cursor.GetInt(ordinal);
        }
        public override int GetInt32(int ordinal)
        {
            return cursor.GetInt(ordinal);
        }
        public override long GetInt64(int ordinal)
        {
            return cursor.GetLong(ordinal);
        }
        public override string GetName(int ordinal)
        {
            return cursor.GetColumnName(ordinal);
        }
        public override int GetOrdinal(string name)
        {
            return cursor.GetColumnIndexOrThrow(name);
        }
        public override string GetString(int ordinal)
        {
            return cursor.GetString(ordinal);
        }
        public override object GetValue(int ordinal)
        {
            switch (cursor.GetType(ordinal))
            {
                case FieldType.Null: return null;
                case FieldType.Float: return cursor.GetFloat(ordinal);
                case FieldType.Integer: return cursor.GetInt(ordinal);
                case FieldType.String: return cursor.GetString(ordinal);
                case FieldType.Blob: return cursor.GetBlob(ordinal);
            }

            return null;
        }
        public override int GetValues(object[] values)
        {
            for (int i = 0; i < cursor.ColumnCount; i++)
                values[i] = GetValue(i);

            return cursor.ColumnCount;
        }
        public override bool IsDBNull(int ordinal)
        {
            return cursor.IsNull(ordinal);
        }

        public override bool NextResult()
        {
            return !cursor.IsLast;
        }
        public override bool Read()
        {
            return cursor.MoveToNext();
        }
        public override void Close()
        {
            cursor.Close();
        }
        public override DataTable GetSchemaTable()
        {
            throw new NotImplementedException();
        }
    }

    public class AndroidDatabaseCommand : DbCommand
    {
        public override string CommandText
        {
            get
            {
                return query;
            }
            set
            {
                query = value;
            }
        }
        public override int CommandTimeout
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public override CommandType CommandType
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }
        public override bool DesignTimeVisible
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }
        public override UpdateRowSource UpdatedRowSource
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }
        protected override DbConnection DbConnection
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }
        protected override DbParameterCollection DbParameterCollection
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        protected override DbTransaction DbTransaction
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        private AndroidDatabaseConnection connection;
        private SQLiteDatabase database;
        private string query;
        CancellationSignal cancellationSignal;

        public AndroidDatabaseCommand(AndroidDatabaseConnection connection, SQLiteDatabase database)
        {
            this.connection = connection;
            this.database = database;
        }

        public override void Cancel()
        {
            cancellationSignal?.Cancel();
        }
        [DebuggerHidden]
        public override int ExecuteNonQuery()
        {
            database.ExecSQL(query);
            return 0; // TODO: Return count of affected rows
        }
        [DebuggerHidden]
        public override object ExecuteScalar()
        {
            ICursor cursor = null;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.JellyBean)
            {
                cancellationSignal = new CancellationSignal();

                cursor = database.RawQuery(query, new string[0], cancellationSignal);
                cancellationSignal = null;
            }
            else
                cursor = database.RawQuery(query, new string[0]);

            AndroidDatabaseReader reader = new AndroidDatabaseReader(cursor);

            if (!reader.Read())
                throw new Exception("The provided query did not yield any result");
            if (reader.FieldCount != 1)
                throw new Exception("The provided query did return more than one column");

            return reader.GetValue(0);
        }
        public override void Prepare()
        {
            throw new NotImplementedException();
        }
        protected override DbParameter CreateDbParameter()
        {
            throw new NotImplementedException();
        }
        [DebuggerHidden]
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            ICursor cursor = null;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.JellyBean)
            {
                cancellationSignal = new CancellationSignal();

                cursor = database.RawQuery(query, new string[0], cancellationSignal);
                cancellationSignal = null;
            }
            else
                cursor = database.RawQuery(query, new string[0]);

            return new AndroidDatabaseReader(cursor);
        }
    }

    public class AndroidDatabaseConnection : DbConnection
    {
        private delegate void AndroidDatabaseHelperUpgradeCallback(SQLiteDatabase database, int oldVersion, int newVersion);

        private class AndroidDatabaseHelper : SQLiteOpenHelper
        {
            public event AndroidDatabaseHelperUpgradeCallback VersionUpgraded;

            public AndroidDatabaseHelper(Context context, string name, int version) : base(context, name, null, version) { }

            public override void OnCreate(SQLiteDatabase db)
            {
                if (VersionUpgraded != null)
                    VersionUpgraded(db , - 1, db.Version);
            }
            public override void OnUpgrade(SQLiteDatabase db, int oldVersion, int newVersion)
            {
                if (VersionUpgraded != null)
                    VersionUpgraded(db, oldVersion, newVersion);
            }
            public override void OnDowngrade(SQLiteDatabase db, int oldVersion, int newVersion)
            {
                if (VersionUpgraded != null)
                    VersionUpgraded(db, oldVersion, newVersion);
            }
        }

        public event AndroidDatabaseUpgradeCallback VersionUpgraded;

        public override string ConnectionString
        {
            get
            {
                return string.Join(";", parameters.Select(p => p.Key + "=" + p.Value));
            }
            set
            {
                string[] parts = value.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                parameters.Clear();
                foreach (string part in parts)
                {
                    int separator = part.IndexOf('=');
                    if (separator == -1)
                        throw new FormatException("Connection string has not a valid format");

                    parameters.Add(part.Substring(0, separator).ToLower(), part.Substring(separator + 1));
                }
            }
        }
        public override string Database
        {
            get
            {
                string name = null;
                parameters.TryGetValue("name", out name);
                return name;
            }
        }
        public override string DataSource
        {
            get
            {
                string name = null;
                parameters.TryGetValue("name", out name);
                return name;
            }
        }
        public override string ServerVersion
        {
            get
            {
                string version = null;
                parameters.TryGetValue("version", out version);
                return version;
            }
        }
        public override ConnectionState State
        {
            get
            {
                return databaseHelper == null ? ConnectionState.Open : ConnectionState.Closed;
            }
        }

        private Context context;
        private Dictionary<string, string> parameters = new Dictionary<string, string>();

        private AndroidDatabaseHelper databaseHelper;
        private SQLiteDatabase sqliteDatabase;

        public AndroidDatabaseConnection(Context context, string name)
        {
            this.context = context;

            parameters.Clear();
            parameters.Add("name", name);
        }
        public AndroidDatabaseConnection(Context context, string name, int version)
        {
            this.context = context;

            parameters.Clear();
            parameters.Add("name", name);
            parameters.Add("version", version.ToString());
        }

        public override void ChangeDatabase(string databaseName)
        {
            throw new NotSupportedException();
        }
        public override void Close()
        {
            databaseHelper.Close();
        }
        public override void Open()
        {
            string name, versionString;
            int version = 1;

            if (!parameters.TryGetValue("name", out name))
                throw new KeyNotFoundException("Name is missing from connection string");
            if (parameters.TryGetValue("version", out versionString) && !int.TryParse(versionString, out version))
                throw new FormatException("Version is not valid");

            databaseHelper = new AndroidDatabaseHelper(context, name, version);
            databaseHelper.VersionUpgraded += DatabaseHelper_VersionUpgraded;

            sqliteDatabase = databaseHelper.WritableDatabase;
        }

        private void DatabaseHelper_VersionUpgraded(SQLiteDatabase database, int oldVersion, int newVersion)
        {
            sqliteDatabase = database;

            if (VersionUpgraded != null)
                VersionUpgraded(this, oldVersion, newVersion);
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            throw new NotSupportedException();
        }
        protected override DbCommand CreateDbCommand()
        {
            return new AndroidDatabaseCommand(this, sqliteDatabase);
        }
    }
}