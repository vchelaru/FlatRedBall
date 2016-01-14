using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IGenerateCodeCommands
    {
        void GenerateAllCode();

        void GenerateCurrentElementCode();

        void GenerateElementCode(IElement element);

        void GenerateGlobalContentCode();

        string GetNamespaceForElement(IElement element);

        void GenerateCurrentCsvCode();

        void GenerateAllCodeSync();

        void GenerateCustomClassesCode();

    }
}
