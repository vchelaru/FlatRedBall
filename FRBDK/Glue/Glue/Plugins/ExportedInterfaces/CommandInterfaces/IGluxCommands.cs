using System;
using EditorObjects.SaveClasses;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.ViewModels;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IGluxCommands
    {
        IScreenCommands ScreenCommands
        {
            get;
        }

        IEntityCommands EntityCommands
        {
            get;
        }

        void SaveGlux(bool sendPluginRefreshCommand = true);

        ReferencedFileSave AddReferencedFileToGlobalContent(string fileToAdd, bool useFullPathAsName);

        ReferencedFileSave AddSingleFileTo(string fileName, string rfsName, string extraCommandLineArguments,
            BuildToolAssociation buildToolAssociation, bool isBuiltFile, string options, 
            IElement sourceElement, string directoryOfTreeNode);

        bool MoveEntityToDirectory(EntitySave entitySave, string newRelativeDirectory);


        // was:
        // SourceType sourceType, string sourceClassType, string sourceFile, string objectName, string sourceNameInFile, string sourceClassGenericType
        NamedObjectSave AddNewNamedObjectToSelectedElement(
            AddObjectViewModel addObjectViewModel);

        ValidationResponse AddNewCustomClass(string className, out CustomClassSave customClassSave);

        void SetVariableOn(NamedObjectSave nos, string memberName, Type memberType, object value);
        void SaveSettings();
    }
}
