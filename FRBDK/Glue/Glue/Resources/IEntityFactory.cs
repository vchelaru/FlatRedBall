using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace REPLACED_NAMESPACE
{
    public interface IEntityFactory
    {
        object CreateNew(float x = 0, float y = 0);
        object CreateNew(Microsoft.Xna.Framework.Vector3 position);
        object CreateNew(FlatRedBall.Graphics.Layer layer);

        void Initialize(string contentManager);
        void ClearListsToAddTo();

        System.Collections.Generic.List<System.Collections.IList> ListsToAddTo { get; }
    }


    public static class FactoryManager
    {
        static Dictionary<string, IEntityFactory> factoryDictionary = new Dictionary<string, IEntityFactory>();
        public static IEntityFactory Get(string entityName)
        {
            if (factoryDictionary.ContainsKey(entityName))
            {
                return factoryDictionary[entityName];
            }
            else
            {
                var factory = FindFactory(entityName);
                factoryDictionary.Add(entityName, factory);
                return factory;
            }
        }

        static Type[] typesInThisAssembly;
        public static IEntityFactory FindFactory(string entityType)
        {
            if (typesInThisAssembly == null)
            {
#if WINDOWS_8 || UWP
                var assembly = typeof(TileEntityInstantiator).GetTypeInfo().Assembly;
                typesInThisAssembly = assembly.DefinedTypes.Select(item=>item.AsType()).ToArray();

#else
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                typesInThisAssembly = assembly.GetTypes();
#endif
            }


#if WINDOWS_8 || UWP
            var filteredTypes =
                typesInThisAssembly.Where(t => t.GetInterfaces().Contains(typeof(IEntityFactory))
                            && t.GetConstructors().Any(c=>c.GetParameters().Count() == 0));
#else
            var filteredTypes =
                typesInThisAssembly.Where(t => t.GetInterfaces().Contains(typeof(IEntityFactory))
                            && t.GetConstructor(Type.EmptyTypes) != null);
#endif

            var factories = filteredTypes
                .Select(
                    t =>
                    {
#if WINDOWS_8 || UWP
                        var propertyInfo = t.GetProperty("Self");
#else
                        var propertyInfo = t.GetProperty("Self");
#endif
                        var value = propertyInfo.GetValue(null, null);
                        return value as IEntityFactory;
                    }).ToList();


            var factory = factories.FirstOrDefault(item =>
            {
                var type = item.GetType();
                var methodInfo = type.GetMethod("CreateNew", new[] { typeof(FlatRedBall.Graphics.Layer), typeof(float), typeof(float) });
                var returntypeString = methodInfo.ReturnType.Name;

                return entityType == returntypeString ||
                    entityType.EndsWith("\\" + returntypeString) ||
                    entityType.EndsWith("/" + returntypeString);
            });
            return factory;

        }


    }
}
