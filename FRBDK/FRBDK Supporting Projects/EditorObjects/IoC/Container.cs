using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EditorObjects.IoC
{
    public class Container
    {
        static Dictionary<Type, object> mObjects = new Dictionary<Type, object>();

        public static void Set<T>(T instance)
        {
            mObjects[typeof(T)] = instance;
        }

        public static void Set(Type type, object instance)
        {
            mObjects[type] = instance;
        }

        public static T Get<T>()
        {
            return (T)mObjects[typeof(T)];
        }

        public static string GetDiagnosticInfo()
        {
            string toReturn = mObjects.Count + " objects contained";

            return toReturn;
        }
    }
}
