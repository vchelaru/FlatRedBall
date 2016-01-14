using System.Collections.Generic;
using System.Reflection;
namespace System
{
    public static class TypeExtensionMethods
    {
        public static PropertyInfo GetProperty(this Type type, string name)
        {
            return type.GetRuntimeProperty(name);
        }
        public static IEnumerable<PropertyInfo> GetProperties(this Type type)
        {
            return type.GetRuntimeProperties();
        }
        public static FieldInfo GetField(this Type type, string name)
        {
            return type.GetRuntimeField(name);
        }
        public static IEnumerable<FieldInfo> GetFields(this Type type)
        {
            return type.GetRuntimeFields();

        }

        public static bool IsPrimitive(this Type type)
        {
            return type.GetTypeInfo().IsPrimitive;
        }

        public static bool IsValueType(this Type type)
        {
            return type.GetTypeInfo().IsValueType;
        }

        public static bool IsEnum(this Type type)
        {
            return type.GetTypeInfo().IsEnum;
        }
        public static ConstructorInfo GetConstructor(this Type type, Type[] types)
        {
            foreach(var constructor in type.GetTypeInfo().DeclaredConstructors)
            {
                var parameters = constructor.GetParameters();

                if(types.Length == parameters.Length)
                {
                    bool mismatch = false;
                    for(int i = 0; i < types.Length; i++)
                    {

                        if(types[i].FullName != parameters[i].ParameterType.FullName)
                        {
                            mismatch = true;
                            break;
                        }
                    }

                    if(!mismatch)
                    {
                        return constructor;
                    }
                }
            }
            return null;
        }

        public static MethodInfo GetMethod(this Type type, string name)
        {
            foreach (var method in type.GetRuntimeMethods())
            {
                if (method.Name == name)
                {
                    return method;
                }
            }
            return null;
        }

        public static MethodInfo GetMethod(this Type type, string name, Type[] parameters)
        {
            return type.GetRuntimeMethod(name, parameters);
        }
        public static MemberInfo[] GetMember(this Type type, string name)
        {
            throw new NotImplementedException();
        }

        public static Type[] GetGenericArguments(this Type type)
        {
            return type.GenericTypeArguments;
            //throw new NotImplementedException();

        }
    }
}