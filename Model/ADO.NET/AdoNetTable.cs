#if MODEL_ADONET_SQLITE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;

public class AdoNetTable<T> : ObjectCollection<T>
{
    public AdoNetConnection Connection { get; }

    public ObjectField[] Fields { get; }

    public AdoNetTable(AdoNetConnection connection, string name, params ObjectField[] fields) : base(name)
    {
        Connection = connection;
        Fields = fields;
    }

    public override void Load()
    {
    }

    protected virtual void EmptyTable()
    {
        string query = "DELETE FROM " + Name;

        using (DbCommand command = Connection.Connection.CreateCommand(query))
            command.ExecuteNonQuery();
    }
    protected virtual void CreateTable()
    {
        string query = "CREATE TABLE " + Name + " (" + Fields.Select(f => f.Name + " " + GetTypeName(f.Type)).Join(", ") + ")";

        using (DbCommand command = Connection.Connection.CreateCommand(query))
            command.ExecuteNonQuery();
    }
    protected virtual bool CheckTable()
    {
        string query = "SELECT * FROM " + Name + " LIMIT 0";

        using (DbCommand command = Connection.Connection.CreateCommand(query))
        {
            using (DbDataReader reader = command.ExecuteReader())
            {
                for (int i = 0; i < Fields.Length; i++)
                {
                    ObjectField field = Fields[i];

                    if (reader.GetName(i) != field.Name)
                        return false;
                    if (reader.GetFieldType(i) != field.Type)
                        return false;
                }
            }
        }

        return true;
    }

    protected virtual string GetTypeName(Type type)
    {
        if (type == typeof(int)) return "INTEGER";
        if (type == typeof(string)) return "VARCHAR";

        throw new NotSupportedException($"Type {type.FullName} is not supported");
    }
}

#endif