using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.IO;
using FlatRedBall.Glue.SaveClasses;
using System.Collections.Generic;
using System.Linq;

namespace FlatRedBall.Glue.CodeGeneration.Game1
{
    /// <summary>
    /// Generates the registry that lets a game find entity factories by entity name without reflection. Native
    /// AOT and trimming remove the constructors and Self properties that FactoryManager's reflection lookup
    /// needs, so Game1.Generated.cs registers every factory with FactoryManager.Register. Names that are not
    /// registered still fall back to reflection at runtime.
    /// </summary>
    public static class EntityFactoryRegistryCodeGenerator
    {
        /// <summary>Called from GeneratedInitializeEarly, before anything can create an entity.</summary>
        public const string RegisterMethodName = "RegisterEntityFactories";

        public static bool IsEnabled(GlueProjectSave project) =>
            project != null && project.FileVersion >= (int)GlueProjectSave.GluxVersions.EntityFactoryRegistry;

        /// <summary>
        /// The entities that have a generated factory: created by other entities and not abstract (an abstract
        /// entity has no factory, matching CodeWriter).
        /// </summary>
        public static List<EntitySave> GetEntitiesWithFactories(GlueProjectSave project)
        {
            return project.Entities
                .Where(item => item.CreatedByOtherEntities && !item.IsAbstract)
                .ToList();
        }

        // The factories in the Game1.Generated.cs that was last written, so an edit that changes the set of
        // factories (add, remove, becoming abstract) can tell Game1.Generated.cs is out of date.
        static List<string> lastGeneratedNames;

        static List<string> GetNames(GlueProjectSave project, string projectNamespace) =>
            GetEntitiesWithFactories(project).Select(item => item.GetQualifiedName(projectNamespace)).ToList();

        /// <summary>
        /// True if the registry in Game1.Generated.cs no longer matches the project's factories.
        /// </summary>
        public static bool IsRegistryStale(GlueProjectSave project, string projectNamespace)
        {
            if (!IsEnabled(project))
            {
                return false;
            }

            var current = GetNames(project, projectNamespace);
            return lastGeneratedNames == null || !current.OrderBy(item => item).SequenceEqual(lastGeneratedNames.OrderBy(item => item));
        }

        public static void GenerateClassScope(ICodeBlock classBlock, GlueProjectSave project, string projectNamespace)
        {
            var entities = GetEntitiesWithFactories(project);
            lastGeneratedNames = GetNames(project, projectNamespace);

            // Direct references to each factory's Self (which news up the factory) are what keep the factory
            // alive under the trimmer. A static constructor on the factory itself would not run if trimmed.
            var register = classBlock.Function("private static void", RegisterMethodName, "");
            foreach (var entity in entities)
            {
                var factoryName = FileManager.RemovePath(entity.Name) + "Factory";
                register.Line(
                    $"global::{projectNamespace}.Performance.FactoryManager.Register(\"{entity.GetQualifiedName(projectNamespace)}\", " +
                    $"global::{projectNamespace}.Factories.{factoryName}.Self);");
            }
        }
    }
}
