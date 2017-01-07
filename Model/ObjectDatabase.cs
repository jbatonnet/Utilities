using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

public abstract class ObjectDatabase
{
    public abstract ObjectCollection Open(string name);
    public abstract ObjectCollection<T> Open<T>(string name);

    public abstract ObjectCollection Create(string name);
    public abstract ObjectCollection<T> Create<T>(string name);
}