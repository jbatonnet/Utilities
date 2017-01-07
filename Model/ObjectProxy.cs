using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Text;

public class ObjectProxy<T> : RealProxy where T : class
{
    public T Object { get; }

    private PropertyInfo[] properties;

    public ObjectProxy(T obj) : base(typeof(T))
    {
        Object = obj;

        Type type = typeof(T);
        properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
    }

    public override IMessage Invoke(IMessage message)
    {
        IMethodCallMessage methodCallMessage = message as IMethodCallMessage;
        if (methodCallMessage != null)
        {
            MethodBase method = methodCallMessage.MethodBase;
            OnMethodCall(method);

            foreach (PropertyInfo property in properties)
            {
                if (property.GetMethod == method)
                    OnPropertyGet(property);
                else if (property.SetMethod == method)
                    OnPropertySet(property);
            }
        }

        return ChannelServices.SyncDispatchMessage(message);
    }

    protected virtual void OnMethodCall(MethodBase method) { }
    protected virtual void OnPropertyGet(PropertyInfo property) { }
    protected virtual void OnPropertySet(PropertyInfo property) { }

    protected virtual void OnCollectionChange(PropertyInfo property) { }
}