using System;
using System.Collections.Generic;

namespace EditorObjects.IoC;

public class Container
{
    private static readonly Dictionary<Type, object> mObjects = new ();

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
}