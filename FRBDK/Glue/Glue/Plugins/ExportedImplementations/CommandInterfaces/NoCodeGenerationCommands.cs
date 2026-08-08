using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    /// <summary>
    /// The <see cref="IGenerateCodeCommands"/> Glue uses for a project it does not generate code for -
    /// today that means FlatRedBall 2 projects, which build their Screens and Entities from Glue's
    /// .gluj/.glsj/.glej JSON at runtime instead. Every "generate" call does nothing.
    /// </summary>
    /// <remarks>
    /// Chosen over conditionals at the call sites on purpose: code generation is kicked off from
    /// dozens of places across Glue and its plugins, so a check at each one would be missed somewhere,
    /// and a missed one writes files into a user's project that should not be there. Selecting the
    /// implementation once (see <see cref="GlueCommands.GenerateCodeCommands"/>) cannot be missed.
    ///
    /// The query methods are not generation and several non-codegen callers depend on them, so they
    /// delegate to the real implementation rather than returning null.
    /// </remarks>
    internal class NoCodeGenerationCommands : IGenerateCodeCommands
    {
        readonly IGenerateCodeCommands _queries;

        public NoCodeGenerationCommands(IGenerateCodeCommands queries)
        {
            _queries = queries;
        }

        public void GenerateAllCode() { }

        public void GenerateCurrentElementCode() { }

        public void GenerateElementCode(GlueElement element, bool generateDerivedElements = true) { }

        public void GenerateElementCustomCode(GlueElement element) { }

        public void GenerateGlobalContentCode() { }

        public void GenerateGlobalContentCodeTask() { }

        public void GenerateElementAndReferencedObjectCode(GlueElement element) { }

        public void GenerateCurrentCsvCode() { }

        public void GenerateCustomClassesCode() { }

        public void GenerateStartupScreenCode() { }

        public void GenerateGame1() { }

        public string GetNamespaceForElement(GlueElement element) => _queries.GetNamespaceForElement(element);

        public string GetNamespaceForElementName(string elementName) => _queries.GetNamespaceForElementName(elementName);

        public string ReplaceGlueVersionString(string contents) => _queries.ReplaceGlueVersionString(contents);
    }
}
