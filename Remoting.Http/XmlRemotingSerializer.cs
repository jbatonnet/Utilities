using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Utilities.Remoting.Http
{
    internal abstract class XmlRemotingSerializer : RemotingSerializer
    {
        private static Lazy<MethodInfo> toArrayMethod = new Lazy<MethodInfo>(() => typeof(Enumerable).GetMethod("ToArray"));

        internal virtual XElement WrapException(Exception exception, string name = null)
        {
            XElement exceptionElement = new XElement(name ?? "Exception");
            Type exceptionType = exception.GetType();

            exceptionElement.Add(new XAttribute("Type", exceptionType.FullName));
            exceptionElement.Add(new XElement("Message", exception.Message));
            exceptionElement.Add(new XElement("StackTrace", exception.StackTrace));

            AggregateException aggregateException = exception as AggregateException;
            if (aggregateException != null)
            {
                foreach (Exception subException in aggregateException.InnerExceptions)
                    exceptionElement.Add(WrapException(subException, "AggregatedException"));
            }

            if (exception.InnerException != null)
                exceptionElement.Add(WrapException(exception.InnerException, "InnerException"));

            return exceptionElement;
        }
        internal virtual Exception UnwrapException(XElement element)
        {
            XAttribute typeAttribute = element.Attribute("Type");
            Type type = ResolveType(typeAttribute.Value);

            XElement messageElement = element.Element("Message");
            string message = messageElement.Value;

            XElement stackTraceElement = element.Element("StackTrace");
            string stackTrace = stackTraceElement.Value;

            // TODO: Rebuild exception
            return new Exception(message + stackTrace);
        }

        internal virtual XElement WrapObject(object value)
        {
            if (value == null)
                return new XElement("Null");

            Type type = value.GetType();

            if (type == typeof(string))
            {
                XElement valueElement = new XElement("Value");

                valueElement.Add(new XAttribute("Type", type.FullName));
                valueElement.Add((string)value);

                return valueElement;
            }

            TypeConverter typeConverter = TypeDescriptor.GetConverter(type);

            if (type.IsValueType)
            {
                // Let's be sure that we can convert back to structure
                if (typeConverter.CanConvertFrom(typeof(string)))
                {
                    XElement valueElement = new XElement("Value");

                    valueElement.Add(new XAttribute("Type", type.FullName));
                    valueElement.Add(typeConverter.ConvertToString(value));

                    return valueElement;
                }

                // Else try to send structure members
                else
                {
                    XElement structElement = new XElement("Struct");

                    structElement.Add(new XAttribute("Type", type.FullName));

                    foreach (FieldInfo field in type.GetFields())
                    {
                        structElement.Add(new XElement("Field",
                            new XAttribute("Name", field.Name),
                            WrapObject(field.GetValue(value))
                        ));
                    }
                    foreach (PropertyInfo property in type.GetProperties().Where(p => p.SetMethod != null))
                    {
                        structElement.Add(new XElement("Property",
                            new XAttribute("Name", property.Name),
                            WrapObject(property.GetValue(value))
                        ));
                    }

                    return structElement;
                }
            }

            // Exception for byte array, to quickly transfer big buffers
            if (type == typeof(byte[]))
            {
                XElement bufferElement = new XElement("Buffer");

                bufferElement.Value = Convert.ToBase64String((byte[])value);

                return bufferElement;
            }

            // Enumerate values before sending them
            if (!type.IsArray && typeof(IEnumerable).IsAssignableFrom(type))
            {
                Type genericEnumerable = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                if (genericEnumerable != null)
                {
                    Type genericArgument = genericEnumerable.GetGenericArguments().First();

                    MethodInfo toArrayMethodInfo = toArrayMethod.Value;
                    MethodInfo genericToArrayMethodInfo = toArrayMethodInfo.MakeGenericMethod(genericArgument);

                    value = genericToArrayMethodInfo.Invoke(null, new object[] { value });
                    type = genericToArrayMethodInfo.ReturnType;
                }
            }

            if (type.IsArray)
            {
                XElement arrayElement = new XElement("Array");
                Array array = value as Array;

                arrayElement.Add(new XAttribute("Type", type.GetElementType().FullName));

                int length = array.GetLength(0);
                arrayElement.Add(new XAttribute("Length", length));

                for (int i = 0; i < length; i++)
                    arrayElement.Add(WrapObject(array.GetValue(i)));

                return arrayElement;
            }

            throw new NotSupportedException("Unable to wrap object of type " + type.FullName);
        }
        internal virtual object UnwrapObject(XElement element)
        {
            if (element == null)
                return null;

            switch (element.Name.LocalName)
            {
                case "Null": return null;

                case "Value":
                {
                    XAttribute typeAttribute = element.Attribute("Type");

                    Type type = ResolveType(typeAttribute.Value);
                    TypeConverter typeConverter = TypeDescriptor.GetConverter(type);

                    return typeConverter.ConvertFromString(element.Value);
                }

                case "Struct":
                case "Structure":
                {
                    XAttribute typeAttribute = element.Attribute("Type");

                    Type type = ResolveType(typeAttribute.Value);
                    object value = Activator.CreateInstance(type);

                    foreach (XElement fieldElement in element.Elements("Field"))
                    {
                        XAttribute nameAttribute = fieldElement.Attribute("Name");
                        string name = nameAttribute.Value;

                        FieldInfo field = type.GetField(name);
                        field.SetValue(value, UnwrapObject(fieldElement.Element("Value")));
                    }

                    foreach (XElement propertyElement in element.Elements("Property"))
                    {
                        XAttribute nameAttribute = propertyElement.Attribute("Name");
                        string name = nameAttribute.Value;

                        PropertyInfo property = type.GetProperty(name);
                        property.SetValue(value, UnwrapObject(propertyElement.Element("Value")));
                    }

                    return value;
                }

                case "Buffer":
                {
                    return Convert.FromBase64String(element.Value);
                }

                case "Array":
                {
                    XAttribute typeAttribute = element.Attribute("Type");
                    XAttribute lengthAttribute = element.Attribute("Length");

                    Type type = ResolveType(typeAttribute.Value);
                    int length = int.Parse(lengthAttribute.Value);

                    Array array = Array.CreateInstance(type, length);
                    for (int i = 0; i < length; i++)
                        array.SetValue(UnwrapObject(element.Elements().ElementAt(i)), i); // FIXME

                    return array;
                }
            }

            throw new NotSupportedException();
        }
    }
}