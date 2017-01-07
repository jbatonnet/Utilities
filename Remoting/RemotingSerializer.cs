using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Utilities.Remoting
{
    using RemoteId = Int32;

    public abstract class RemotingSerializer
    {
        public static Type ResolveType(string name)
        {
            return Type.GetType(name) ?? AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).FirstOrDefault(t => t != null);
        }

        protected Delegate CreateDelegate(Type delegateType, RemoteId remoteId)
        {
            Type type = GetType();
            MethodInfo invokeMethod = delegateType.GetMethod("Invoke");

            List<Type> parameterTypes = invokeMethod.GetParameters().Select(p => p.ParameterType).ToList();
            parameterTypes.Insert(0, type);

            MethodInfo delegateProxy = type.GetMethod(nameof(OnDelegateCall), BindingFlags.NonPublic | BindingFlags.Instance);

            DynamicMethod dynamicMethod = new DynamicMethod("", invokeMethod.ReturnType, parameterTypes.ToArray(), type);
            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldc_I4, remoteId);

            if (parameterTypes.Count == 0)
                ilGenerator.Emit(OpCodes.Ldnull); // args
            else
            {
                ilGenerator.Emit(OpCodes.Ldc_I4, parameterTypes.Count - 1);
                ilGenerator.Emit(OpCodes.Newarr, typeof(object));

                for (int i = 0; i < parameterTypes.Count - 1; i++)
                {
                    ilGenerator.Emit(OpCodes.Dup);
                    ilGenerator.Emit(OpCodes.Ldc_I4, i);
                    ilGenerator.Emit(OpCodes.Ldarg, i + 1);

                    if (parameterTypes[i].IsValueType)
                        ilGenerator.Emit(OpCodes.Box, parameterTypes[i + 1]);

                    ilGenerator.Emit(OpCodes.Stelem_Ref);
                }
            }

            ilGenerator.EmitCall(OpCodes.Call, delegateProxy, null);

            if (invokeMethod.ReturnType == typeof(void))
                ilGenerator.Emit(OpCodes.Pop);
            else if (invokeMethod.ReturnType.IsValueType)
                ilGenerator.Emit(OpCodes.Unbox_Any, invokeMethod.ReturnType);

            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate(delegateType, this);
        }
        protected virtual object OnDelegateCall(RemoteId remoteId, object[] args)
        {
            return null;
        }
    }
}