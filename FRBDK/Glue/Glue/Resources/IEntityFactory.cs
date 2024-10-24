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
        int NewInstancesCreatedThisScreen { get; }
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

        /// <summary>
        /// Gets a Factory for the unqualified entity name. For example, to get a factory for an Entity named 
        /// "Enemy", pass the argument "Enemy" without the prefix "Entities/"
        /// </summary>
        /// <param name="entityName">The unqualified Entity name.</param>
        /// <returns>An IFactory instance for the argument entity name.</returns>
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

        public static IEntityFactory Get(Type type) => Get(type.Name);

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
                var returnTypeString = methodInfo.ReturnType.Name;

                return entityType == returnTypeString ||
                    entityType.EndsWith("\\" + returnTypeString) ||
                    entityType.EndsWith("/" + returnTypeString) ||
                    entityType.EndsWith("." + returnTypeString);
            });
            return factory;

        }

        private static void FillWithFactories()
        {
            if (typesInThisAssembly == null)
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                typesInThisAssembly = assembly.GetTypes();
            }


            var filteredTypes =
                typesInThisAssembly.Where(t => t.GetInterfaces().Contains(typeof(IEntityFactory))
                            && t.GetConstructor(Type.EmptyTypes) != null);

            Factories = filteredTypes
                .Select(
                    t =>
                    {
                        var propertyInfo = t.GetProperty("Self");
                        var value = propertyInfo.GetValue(null, null);
                        return value as IEntityFactory;
                    }).ToList();
        }

        static StringBuilder creationReportBuilder = new StringBuilder();
        public static string GetCreationReport(bool includeFactoriesWith0Instances = false)
        {
            creationReportBuilder.Clear();
            var allFactories = GetAllFactories()
                .OrderByDescending(item => item.NewInstancesCreatedThisScreen);
            foreach (var factory in allFactories)
            {
                if (includeFactoriesWith0Instances || factory.NewInstancesCreatedThisScreen > 0)
                {
                    creationReportBuilder.AppendLine($"{factory.GetType().Name}:{factory.NewInstancesCreatedThisScreen:N0}");
                }
            }

            return creationReportBuilder.ToString();
        }
    }
}
