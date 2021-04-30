using System;
using FlatRedBall.Glue.SaveClasses;
using System.Collections.Generic;
using GlueFormsCore.ViewModels;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IGluxCommands
    {
        #region Fields/Properties

        IScreenCommands ScreenCommands
        {
            get;
        }

        IEntityCommands EntityCommands
        {
            get;
        }

        IProjectCommands ProjectCommands { get; }

        string StartUpScreenName { get; set; }

        #endregion

        #region Glux Methods
        /// <summary>
        /// Saves the glue project immediately if in a task, and adds a task if not
        /// </summary>
        void SaveGlux();

        void SaveSettings();

        #endregion

        #region ReferencedFileSave

        ReferencedFileSave CreateNewFileAndReferencedFileSave(AddNewFileViewModel viewModel, object creationOptions = null);


        /// <summary>
        /// Adds a new file to the Glue project in global content. This method updates the in-memory GlueProjectSave,
        /// adds the file to the Visual Studio project, and refreshes the global content tree node.
        /// This method does not save the .glux.
        /// </summary>
        /// <param name="fileToAdd">The file name relative to the Glue project. 
        /// For example, to add a png file to GlobalContent, the file name might be "GlobalContent/MyFile.png"</param>
        /// <param name="includeDirectoryInGlobalContentInName">Whether to include the subdirectory 
        /// in the name of the newly-created file.</param>
        /// <returns>The new ReferencedFileSave.</returns>
        ReferencedFileSave AddReferencedFileToGlobalContent(string fileToAdd, bool includeDirectoryInGlobalContentInName);
        void AddReferencedFileToGlobalContent(ReferencedFileSave rfs);
        void AddReferencedFileToElement(ReferencedFileSave rfs, IElement element);

        ReferencedFileSave GetReferencedFileSaveFromFile(string fileName);

        ReferencedFileSave AddSingleFileTo(string fileName, string rfsName, string extraCommandLineArguments,
            EditorObjects.SaveClasses.BuildToolAssociation buildToolAssociation, bool isBuiltFile, object options, 
            IElement sourceElement, string directoryOfTreeNode, bool selectFileAfterCreation = true);

        void RemoveReferencedFile(ReferencedFileSave referencedFileToRemove, List<string> additionalFilesToRemove, bool regenerateCode = true);

        #endregion
        


        bool SetPluginRequirement(Interfaces.IPlugin plugin, bool requiredByProject);
        bool SetPluginRequirement(string name, bool requiredByProject, Version version);
        bool GetPluginRequirement(Interfaces.IPlugin plugin);

        #region Entity

        bool MoveEntityToDirectory(EntitySave entitySave, string newRelativeDirectory);

        void RemoveEntity(EntitySave entityToRemove, List<string> filesThatCouldBeRemoved = null);

        #endregion

        ValidationResponse AddNewCustomClass(string className, out CustomClassSave customClassSave);

        #region Screens

        void RemoveScreen(ScreenSave screenToRemove, List<string> filesThatCouldBeRemoved = null);

        #endregion

        #region Named Objects

        // SourceType sourceType, string sourceClassType, string sourceFile, string objectName, string sourceNameInFile, string sourceClassGenericType
        NamedObjectSave AddNewNamedObjectToSelectedElement(ViewModels.AddObjectViewModel addObjectViewModel);
        NamedObjectSave AddNewNamedObjectTo(ViewModels.AddObjectViewModel addObjectViewModel, IElement element, NamedObjectSave listToAddTo = null);

        void AddNamedObjectTo(NamedObjectSave newNos, IElement element, NamedObjectSave listToAddTo = null);

        [Obsolete("I don't think we need the typed version anymore. Use the untyped version")]
        void SetVariableOn(NamedObjectSave nos, string memberName, Type memberType, object value);

        void SetVariableOn(NamedObjectSave nos, string memberName, object value);

        void RemoveNamedObject(NamedObjectSave namedObjectToRemove, bool performSave = true, bool updateUi = true,
            List<string> additionalFilesToRemove = null);
        #endregion

        #region Import

        GlueElement ImportScreenOrEntityFromFile(FilePath filePath);

        #endregion
    }
}
