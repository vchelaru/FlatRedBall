using FlatRedBall.Glue.SaveClasses;
using System;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IGenerateCodeCommands
    {
        void GenerateAllCodeTask();
        void GenerateAllCode();

        /// <summary>
        /// Generates the current element's code, using
        /// TaskExecutionPreference.AddOrMoveToEnd
        /// </summary>
        void GenerateCurrentElementCode();

        void GenerateElementCode(GlueElement element);
        [Obsolete("Use GenerateElementCode because that will automatically task if necessary")]
        void GenerateElementCodeTask(GlueElement element);

        void GenerateGlobalContentCode();
        void GenerateGlobalContentCodeTask();

        void GenerateElementAndReferencedObjectCodeTask(GlueElement element);


        string GetNamespaceForElement(GlueElement element);

        void GenerateCurrentCsvCode();

        void GenerateCustomClassesCode();

        void GenerateStartupScreenCode();

        void GenerateGame1();
    }
}
