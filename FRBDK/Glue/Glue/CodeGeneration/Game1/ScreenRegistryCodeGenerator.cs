using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System.Collections.Generic;
using System.Linq;

namespace FlatRedBall.Glue.CodeGeneration.Game1
{
    /// <summary>
    /// Generates the registry that lets a game create screens by name without reflection. Native AOT and
    /// trimming remove screen constructors that only Assembly.GetType + Activator reach, so Game1.Generated.cs
    /// registers one factory per screen with ScreenManager.RegisterScreenFactory and resolves the
    /// command line start screen through the same list. Names that are not registered still fall back to
    /// reflection at runtime.
    /// </summary>
    public static class ScreenRegistryCodeGenerator
    {
        /// <summary>Called from GeneratedInitializeEarly, before anything can start or load a screen.</summary>
        public const string RegisterMethodName = "RegisterScreenFactories";

        /// <summary>Returns the Type for a fully qualified screen name, or null if there is no such screen.</summary>
        public const string GetScreenTypeMethodName = "GetScreenTypeByName";

        public static bool IsEnabled(GlueProjectSave project) =>
            project != null && project.FileVersion >= (int)GlueProjectSave.GluxVersions.ScreenManagerHasRegisterScreenFactory;

        /// <summary>
        /// The fully qualified names of the screens that get a factory. Abstract screens are skipped because
        /// their generated class cannot be constructed.
        /// </summary>
        public static List<string> GetQualifiedScreenNames(GlueProjectSave project, string projectNamespace)
        {
            return project.Screens
                .Where(item => !item.IsAbstract)
                .Select(item => item.GetQualifiedName(projectNamespace))
                .ToList();
        }

        // The screen names in the Game1.Generated.cs that was last written, so an edit that changes the set of
        // registrable screens (add, remove, becoming abstract) can tell Game1.Generated.cs is out of date.
        static List<string> lastGeneratedNames;

        /// <summary>
        /// True if the registry in Game1.Generated.cs no longer matches the project's screens.
        /// </summary>
        public static bool IsRegistryStale(GlueProjectSave project, string projectNamespace)
        {
            if (!IsEnabled(project))
            {
                return false;
            }

            var current = GetQualifiedScreenNames(project, projectNamespace);
            return lastGeneratedNames == null || !current.OrderBy(item => item).SequenceEqual(lastGeneratedNames.OrderBy(item => item));
        }

        /// <summary>
        /// Regenerates Game1.Generated.cs if the screens it registers are out of date. Call after any change
        /// that can alter which screens exist or which are abstract.
        /// </summary>
        public static void RegenerateGame1IfStale()
        {
            var project = GlueState.Self.CurrentGlueProject;
            if (IsRegistryStale(project, GlueState.Self.ProjectNamespace))
            {
                GlueCommands.Self.GenerateCodeCommands.GenerateGame1();
            }
        }

        public static void GenerateClassScope(ICodeBlock classBlock, IEnumerable<string> qualifiedScreenNames)
        {
            var names = qualifiedScreenNames.ToList();
            lastGeneratedNames = names;

            var register = classBlock.Function("private static void", RegisterMethodName, "");
            foreach (var name in names)
            {
                register.Line($"FlatRedBall.Screens.ScreenManager.RegisterScreenFactory(\"{name}\", () => new global::{name}());");
            }

            var getType = classBlock.Function("private static System.Type", GetScreenTypeMethodName, "string screenFullName");
            if (names.Count > 0)
            {
                var switchBlock = getType.Switch("screenFullName");
                foreach (var name in names)
                {
                    switchBlock.CaseNoBreak($"\"{name}\"").Line($"return typeof(global::{name});");
                }
            }
            getType.Line("return GetScreenTypeByReflection(screenFullName);");

            // Kept separate so the suppression covers only the one reflective call.
            classBlock.Line("[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(\"Trimming\", \"IL2026\", " +
                "Justification = \"Only reached for names that are not generated screens, such as screens added by hand or by an older project.\")]");
            var byReflection = classBlock.Function("private static System.Type", "GetScreenTypeByReflection", "string screenFullName");
            byReflection.Line("return System.Reflection.Assembly.GetExecutingAssembly().GetType(screenFullName);");
        }
    }
}
