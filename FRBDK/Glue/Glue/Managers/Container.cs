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
            var type = typeof(T);
#if DEBUG
            if(mObjects.ContainsKey(type) == false)
            {
                throw new InvalidOperationException("The container does not contain an entry for the type " + type);
            }
#endif
            return (T)mObjects[type];
        }

        public static string GetDiagnosticInfo()
        {
            string toReturn = mObjects.Count + " objects contained";

            return toReturn;
        }
    }
}
