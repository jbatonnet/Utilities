using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

[Flags]
public enum ObjectFieldOptions
{
    None,
    Primary,
    AutoIncrement
}

public class ObjectField
{
    public string Name { get; }
    public Type Type { get; }
    public ObjectFieldOptions Options { get; }

    public ObjectField(string name, Type type)
    {
        Name = name;
        Type = type;
        Options = ObjectFieldOptions.None;
    }
    public ObjectField(string name, Type type, ObjectFieldOptions options)
    {
        Name = name;
        Type = type;
        Options = options;
    }
}

public class ObjectFieldMapping
{
    
}