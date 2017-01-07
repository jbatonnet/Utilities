using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

public abstract class ObjectCollection : IEnumerable
{
    public string Name { get; }
    public abstract ICollection Objects { get; }

    public ObjectCollection(string name)
    {
        Name = name;
    }

    public abstract void Load();
    public abstract void Save();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return Objects.GetEnumerator();
    }
}

public abstract class ObjectCollection<T> : ObjectCollection, IEnumerable<T>
{
    //public abstract ICollection<T> Objects { get; }

    public ObjectCollection(string name) : base(name) { }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return null; // Objects?.GetEnumerator();
    }
}