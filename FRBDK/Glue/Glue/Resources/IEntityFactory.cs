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

        void Destroy();


        System.Collections.Generic.List<System.Collections.IList> ListsToAddTo { get; }
    }


    public static class FactoryManager
    {
        static Dictionary<string, IEntityFactory> FactoryDictionary = new Dictionary<string, IEntityFactory>();

        static List<IEntityFactory> Factories = new List<IEntityFactory>();

        public static IEnumerable<IEntityFactory> GetAllFactories()
        {
            if (Factories.Count == 0)
            {
                FillWithFactories();
            }
            return Factories.ToList();
        }

        public static IEntityFactory Get(string entityName)
        {
            if (FactoryDictionary.ContainsKey(entityName))
            {
                return FactoryDictionary[entityName];
            }
            else
            {
                var factory = FindFactory(entityName);
                FactoryDictionary.Add(entityName, factory);
                return factory;
            }
        }

        static Type[] typesInThisAssembly;
        public static IEntityFactory FindFactory(string entityType)
        {
            if (Factories.Count == 0)
            {
                FillWithFactories();
            }
            var factory = Factories.FirstOrDefault(item =>
            {
                var type = item.GetType();
                var methodInfo = type.GetMethod("CreateNew", new[] { typeof(FlatRedBall.Graphics.Layer), typeof(float), typeof(float), typeof(float) });
                var returntypeString = methodInfo.ReturnType.Name;

                return entityType == returntypeString ||
                    entityType.EndsWith("\\" + returntypeString) ||
                    entityType.EndsWith("/" + returntypeString);
            });
            return factory;

        }

        private static void FillWithFactories()
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

            Factories = filteredTypes
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
        }
    }
}
