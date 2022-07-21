using FlatRedBall.Glue.SaveClasses;
using System;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IGenerateCodeCommands
    {
        void GenerateAllCode();

        /// <summary>
        /// Generates the current element's code, using
        /// TaskExecutionPreference.AddOrMoveToEnd
        /// </summary>
        void GenerateCurrentElementCode();

        /// <summary>
        /// Generates the argument element's code. Also generates all elements which inherit from this element.
        /// </summary>
        /// <param name="element"></param>
        void GenerateElementCode(GlueElement element);

        Task GenerateElementCodeAsync(GlueElement element);

        void GenerateGlobalContentCode();
        void GenerateGlobalContentCodeTask();

        Task GenerateElementAndReferencedObjectCode(GlueElement element);


        string GetNamespaceForElement(GlueElement element);

        void GenerateCurrentCsvCode();

        void GenerateCustomClassesCode();

        void GenerateStartupScreenCode();

        void GenerateGame1();
    }
}
