using System.Linq;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    /// <summary>
    /// The <see cref="IGenerateCodeCommands"/> for a FlatRedBall 2 project that has opted into code
    /// generation (<see cref="GlueProjectSave.GenerateCode"/>). Screens and Entities go through
    /// <see cref="Frb2CodeGenerator"/> - FRB1's typed-accessor-free equivalents.
    /// </summary>
    /// <remarks>
    /// A third implementation of the interface rather than branches inside <see cref="GenerateCodeCommands"/>,
    /// for the same reason <see cref="NoCodeGenerationCommands"/> is a separate class and not a
    /// conditional at each call site: <see cref="GenerateCodeCommands"/>'s methods assume FRB1 concepts
    /// this project has no equivalent for yet - Game1.Generated.cs, camera setup, factories, CSV data
    /// classes, global content - and those methods are called from many unrelated places (renames,
    /// element adds, ...) that have no reason to know which project type is loaded. Selecting the
    /// right implementation once, in <see cref="GlueCommands.GenerateCodeCommands"/>, is what makes
    /// every one of those call sites safe automatically instead of needing to be found and checked.
    /// </remarks>
    internal class Frb2GenerateCodeCommands : IGenerateCodeCommands
    {
        // The namespace-computation and $GLUE_VERSIONS$ helpers are pure string logic with nothing
        // FRB1-specific about them - reused rather than duplicated, same choice NoCodeGenerationCommands
        // makes for its own query delegation.
        readonly IGenerateCodeCommands _queries;

        public Frb2GenerateCodeCommands(IGenerateCodeCommands queries)
        {
            _queries = queries;
        }

        public void GenerateAllCode()
        {
            TaskManager.Self.AddOrRunIfTasked(
                GenerateAllCodeSync,
                "Generating all code",
                TaskExecutionPreference.AddOrMoveToEnd);
        }

        void GenerateAllCodeSync()
        {
            var glueProject = GlueState.Self.CurrentGlueProject;

            if (glueProject == null)
            {
                return;
            }

            foreach (var screen in glueProject.Screens)
            {
                if (ProjectManager.WantsToCloseProject)
                {
                    return;
                }

                Frb2CodeGenerator.GenerateCode(screen);
            }

            System.Collections.Generic.List<EntitySave> entityList;
            lock (glueProject.Entities)
            {
                entityList = glueProject.Entities.ToList();
            }

            foreach (var entity in entityList)
            {
                if (ProjectManager.WantsToCloseProject)
                {
                    return;
                }

                Frb2CodeGenerator.GenerateCode(entity);
            }
        }

        public void GenerateCurrentElementCode()
        {
            GlueElement element = GlueState.Self.CurrentElement;

            if (element != null)
            {
                TaskManager.Self.AddAsync(
                    () => Frb2CodeGenerator.GenerateCode(element),
                    $"Generating element {element}",
                    TaskExecutionPreference.AddOrMoveToEnd);
            }
        }

        public void GenerateElementCode(GlueElement element, bool generateDerivedElements = true)
        {
            if (element == null)
            {
                return;
            }

            TaskManager.Self.Add(
                () => Frb2CodeGenerator.GenerateCode(element),
                nameof(GenerateElementCode) + " " + element,
                TaskExecutionPreference.AddOrMoveToEnd);

            if (generateDerivedElements)
            {
                foreach (var derivedElement in ObjectFinder.Self.GetAllElementsThatInheritFrom(element))
                {
                    TaskManager.Self.Add(
                        () => Frb2CodeGenerator.GenerateCode(derivedElement),
                        nameof(GenerateElementCode) + " " + derivedElement,
                        TaskExecutionPreference.AddOrMoveToEnd);
                }
            }
        }

        public void GenerateElementCustomCode(GlueElement element) =>
            Frb2CodeGenerator.GenerateCustomCode(element);

        public void GenerateElementAndReferencedObjectCode(GlueElement element)
        {
            var toRegenerate = new System.Collections.Generic.HashSet<GlueElement>();

            if (element != null)
            {
                toRegenerate.Add(element);

                foreach (var nos in ObjectFinder.Self.GetAllNamedObjectsThatUseElement(element))
                {
                    toRegenerate.Add(ObjectFinder.Self.GetElementContaining(nos));
                }
            }

            foreach (var elementToRegenerate in toRegenerate)
            {
                TaskManager.Self.Add(
                    () => Frb2CodeGenerator.GenerateCode(elementToRegenerate),
                    nameof(GenerateElementAndReferencedObjectCode) + " " + elementToRegenerate,
                    TaskExecutionPreference.AddOrMoveToEnd);
            }
        }

        // No FRB2 equivalent yet - global content, CSV data classes, a generated startup screen, and
        // Game1.Generated.cs are all FRB1 concepts this project's runtime does not have. Left as no-ops
        // rather than reusing FRB1's generators, which would write code against APIs that do not exist
        // here.
        public void GenerateGlobalContentCode() { }

        public void GenerateGlobalContentCodeTask() { }

        public void GenerateCurrentCsvCode() { }

        public void GenerateCustomClassesCode() { }

        public void GenerateStartupScreenCode() { }

        public void GenerateGame1() { }

        public string GetNamespaceForElement(GlueElement element) => _queries.GetNamespaceForElement(element);

        public string GetNamespaceForElementName(string elementName) => _queries.GetNamespaceForElementName(elementName);

        public string ReplaceGlueVersionString(string contents) => _queries.ReplaceGlueVersionString(contents);
    }
}
