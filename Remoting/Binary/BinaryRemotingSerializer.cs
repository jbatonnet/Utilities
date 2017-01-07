using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace Utilities.Remoting
{
    using System.Linq;
    using System.Reflection;
    using RemoteObject = MarshalByRefObject;

    internal enum BinaryRemotingCommand : byte
    {
        Get,
        Call,
        Result,
        Exception,
        Event
    }
    internal enum BinaryRemotingType : byte
    {
        Null,
        Value,
        String,
        RemoteObject,
        Exception,
        Buffer,
        Array,
        Delegate
    }

    internal abstract class BinaryRemotingSerializer : RemotingSerializer
    {
        internal virtual void WriteObject(Stream stream, object value, Type type)
        {
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.Default, true))
            {
                if (value == null)
                {
                    writer.Write((byte)BinaryRemotingType.Null);
                    return;
                }

                Type valueType = value.GetType();
                TypeConverter typeConverter = TypeDescriptor.GetConverter(valueType);

                if (valueType == typeof(string))
                {
                    writer.Write((byte)BinaryRemotingType.String);
                    writer.Write(value as string);

                    return;
                }
                else if (valueType.IsValueType)
                {
                    writer.Write((byte)BinaryRemotingType.Value);
                    WriteType(stream, valueType);
                    writer.Write(typeConverter.ConvertToString(value));

                    return;
                }
                else if (valueType == typeof(byte[]))
                {
                    writer.Write((byte)BinaryRemotingType.Buffer);
                    writer.Write((value as byte[]).Length);
                    writer.Write(value as byte[]);

                    return;
                }
                else if (valueType.IsArray)
                {
                    Array array = value as Array;
                    Type elementType = valueType.GetElementType();
                    int length = array.GetLength(0);

                    writer.Write((byte)BinaryRemotingType.Array);
                    writer.Write(length);
                    WriteType(stream, elementType);

                    for (int i = 0; i < length; i++)
                        WriteObject(stream, array.GetValue(i), elementType);

                    return;
                }
                else if (type.IsGenericType)
                {
                    Type genericTypeDefinition = type.GetGenericTypeDefinition();
                    Type genericTypeParameter = type.GetGenericArguments()[0];

                    Type arrayType = genericTypeParameter.MakeArrayType();

                    if (type.IsAssignableFrom(arrayType))
                    {
                        Type enumerableType = typeof(Enumerable);
                        MethodInfo genericMethod = enumerableType.GetMethod("ToArray");
                        MethodInfo typedMethod = genericMethod.MakeGenericMethod(genericTypeParameter);

                        object array = typedMethod.Invoke(null, new[] { value });
                        WriteObject(stream, array, arrayType);

                        return;
                    }
                }

                throw new NotSupportedException("Unable to wrap object of type " + valueType.FullName);
            }
        }
        internal virtual void WriteException(Stream stream, Exception exception)
        {
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.Default, true))
            {
                writer.Write((byte)BinaryRemotingType.Exception);
                WriteType(stream, exception.GetType());
                writer.Write(exception.Message);
                writer.Write(exception.StackTrace);
            }
        }
        internal virtual void WriteType(Stream stream, Type type)
        {
            List<Type> typeHierarchy = new List<Type>();

            // Build type hierarchy
            while (type.BaseType != null && type != typeof(object))
            {
                typeHierarchy.Add(type);
                type = type.BaseType;
            }

            // Send everything
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.Default, true))
            {
                writer.Write(typeHierarchy.Count);

                foreach (Type typeParent in typeHierarchy)
                    writer.Write(typeParent.FullName);
            }
        }

        internal object ReadObject(Stream stream)
        {
            BinaryRemotingType remotingType = (BinaryRemotingType)stream.ReadByte();
            return ReadObject(stream, remotingType);
        }
        internal virtual object ReadObject(Stream stream, BinaryRemotingType remotingType)
        {
            switch (remotingType)
            {
                case BinaryRemotingType.Null: return null;
                case BinaryRemotingType.Exception: return ReadException(stream);
                case BinaryRemotingType.RemoteObject: return ReadRemoteObject(stream);
            }

            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, true))
            {
                switch (remotingType)
                {
                    case BinaryRemotingType.String:
                        return reader.ReadString();

                    case BinaryRemotingType.Value:
                    {
                        Type type = ReadType(stream);
                        string value = reader.ReadString();

                        TypeConverter typeConverter = TypeDescriptor.GetConverter(type);

                        return typeConverter.ConvertFromString(value);
                    }

                    case BinaryRemotingType.Buffer:
                    {
                        int count = reader.ReadInt32();
                        return reader.ReadBytes(count);
                    }

                    case BinaryRemotingType.Array:
                    {
                        int length = reader.ReadInt32();
                        Type type = ReadType(stream);

                        Array array = Array.CreateInstance(type, length);

                        for (int i = 0; i < length; i++)
                            array.SetValue(ReadObject(stream), i);

                        return array;
                    }
                }

                throw new NotSupportedException("Unable to unwrap object of type " + remotingType);
            }
        }
        internal abstract RemoteObject ReadRemoteObject(Stream stream);
        internal virtual Exception ReadException(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, true))
            {
                Type exceptionType = ReadType(stream);
                string exceptionMessage = reader.ReadString();
                string exceptionStackTrace = reader.ReadString();

                return new Exception(exceptionMessage);
            }
        }
        internal virtual Type ReadType(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, true))
            {
                int typeHierarchySize = reader.ReadInt32();
                List<string> typeHierarchy = new List<string>();

                for (int i = 0; i < typeHierarchySize; i++)
                    typeHierarchy.Add(reader.ReadString());

                foreach (string typeName in typeHierarchy)
                {
                    Type type = ResolveType(typeName);
                    if (type != null)
                        return type;
                }
            }

            return typeof(RemoteObject);
        }
    }
}