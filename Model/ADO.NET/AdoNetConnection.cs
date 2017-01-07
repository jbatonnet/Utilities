#if MODEL_ADONET_SQLITE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Text;

public class AdoNetConnection : ObjectDatabase
{
    public DbConnection Connection { get; protected set; }

    public AdoNetConnection(DbConnection connection)
    {
        Connection = connection;
    }
    public AdoNetConnection(Type connectionType, string connectionString)
    {
        Connection = Activator.CreateInstance(connectionType, connectionString) as DbConnection;
        Connection.Open();
    }

    public override ObjectCollection Create(string name)
    {
        throw new NotSupportedException("You must specify either object type or fields");
    }
    public override ObjectCollection<T> Create<T>(string name)
    {
        Type type = typeof(T);
        List<ObjectField> fields = new List<ObjectField>();

        foreach (PropertyInfo property in type.GetProperties())
            fields.Add(new ObjectField(property.Name, property.PropertyType));

        return Create<T>(name, fields.ToArray());
    }
    public ObjectCollection<T> Create<T>(string name, params ObjectField[] fields)
    {
        return new AdoNetTable<T>(this, name, fields);
    }
}

#endif