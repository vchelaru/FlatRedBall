using System;
using FlatRedBall.Glue.SaveClasses;
using System.Collections.Generic;

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

        IProjectCommands ProjectCommands { get; }

        void SaveGlux(bool sendPluginRefreshCommand = true);

        ReferencedFileSave AddReferencedFileToGlobalContent(string fileToAdd, bool useFullPathAsName);

        ReferencedFileSave GetReferencedFileSaveFromFile(string fileName);

#if GLUE
        ReferencedFileSave AddSingleFileTo(string fileName, string rfsName, string extraCommandLineArguments,
            EditorObjects.SaveClasses.BuildToolAssociation buildToolAssociation, bool isBuiltFile, string options, 
            IElement sourceElement, string directoryOfTreeNode);
        // SourceType sourceType, string sourceClassType, string sourceFile, string objectName, string sourceNameInFile, string sourceClassGenericType
        NamedObjectSave AddNewNamedObjectToSelectedElement(
            ViewModels.AddObjectViewModel addObjectViewModel);
#endif

        bool MoveEntityToDirectory(EntitySave entitySave, string newRelativeDirectory);


        // was:

        ValidationResponse AddNewCustomClass(string className, out CustomClassSave customClassSave);

        void RemoveReferencedFile(ReferencedFileSave referencedFileToRemove, List<string> additionalFilesToRemove);
        void RemoveReferencedFile(ReferencedFileSave referencedFileToRemove, List<string> additionalFilesToRemove, bool regenerateCode);


        void SetVariableOn(NamedObjectSave nos, string memberName, Type memberType, object value);
        void SaveSettings();
    }
}
