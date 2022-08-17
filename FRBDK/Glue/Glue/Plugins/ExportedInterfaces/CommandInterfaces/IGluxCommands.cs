using System;
using FlatRedBall.Glue.SaveClasses;
using System.Collections.Generic;
using GlueFormsCore.ViewModels;
using FlatRedBall.IO;
using FlatRedBall.Glue.Elements;
using System.Threading.Tasks;
using FlatRedBall.Glue.Managers;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public class NosVariableAssignment
    {
        public NamedObjectSave NamedObjectSave;
        public string VariableName;
        public object Value;
    }

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

        IElementCommands ElementCommands { get;  }

        IProjectCommands ProjectCommands { get; }

        string StartUpScreenName { get; set; }

        #endregion

        #region Save Glux Methods
        /// <summary>
        /// Saves the glue project immediately if in a task, and adds a task if not
        /// </summary>
        void SaveGlux(TaskExecutionPreference taskExecutionPreference = TaskExecutionPreference.Asap);

        void SaveSettings();

        #endregion

        #region CustomClass

        ValidationResponse AddNewCustomClass(string className, out CustomClassSave customClassSave);

        #endregion

        #region ReferencedFileSave

        Task<ReferencedFileSave> CreateNewFileAndReferencedFileSaveAsync(AddNewFileViewModel viewModel, GlueElement element, object creationOptions = null);


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
        void AddReferencedFileToElement(ReferencedFileSave rfs, GlueElement element);


        [Obsolete("Use CreateReferencedFileSaveForExistingFileAsync")]
        ReferencedFileSave CreateReferencedFileSaveForExistingFile(GlueElement containerForFile, FilePath filePath, AssetTypeInfo ati = null);
        Task<ReferencedFileSave> CreateReferencedFileSaveForExistingFileAsync(GlueElement containerForFile, FilePath filePath, AssetTypeInfo ati = null);
        [Obsolete("Use GetReferencedFileSaveFromFile which takes a FilePath")]
        ReferencedFileSave GetReferencedFileSaveFromFile(string filePath);
        ReferencedFileSave GetReferencedFileSaveFromFile(FilePath filePath);

        ReferencedFileSave AddSingleFileTo(string fileName, string rfsName, string extraCommandLineArguments,
            EditorObjects.SaveClasses.BuildToolAssociation buildToolAssociation, bool isBuiltFile, object options,
            GlueElement sourceElement, string directoryOfTreeNode, bool selectFileAfterCreation = true);

        void RemoveReferencedFile(ReferencedFileSave referencedFileToRemove, List<string> additionalFilesToRemove, bool regenerateAndSave = true);

        Task DuplicateAsync(ReferencedFileSave rfs, GlueElement forcedContainer = null);
        

        #endregion

        #region Plugin Requirements

        bool SetPluginRequirement(PluginBase plugin, bool requiredByProject);
        bool SetPluginRequirement(string name, bool requiredByProject, Version version);
        bool GetPluginRequirement(PluginBase plugin);

        #endregion

        #region GlueElements

        Task CopyGlueElement(GlueElement original);

        FilePath GetElementJsonLocation(GlueElement element);

        FilePath GetPreviewLocation(GlueElement glueElement, StateSave stateSave);

        #endregion

        #region Entity

        bool MoveEntityToDirectory(EntitySave entitySave, string newRelativeDirectory);

        Task RemoveEntityAsync(EntitySave entityToRemove, List<string> filesThatCouldBeRemoved = null);

        #endregion

        #region Screens

        void RemoveScreen(ScreenSave screenToRemove, List<string> filesThatCouldBeRemoved = null);

        #endregion

        #region Named Objects

        // SourceType sourceType, string sourceClassType, string sourceFile, string objectName, string sourceNameInFile, string sourceClassGenericType
        Task<NamedObjectSave> AddNewNamedObjectToSelectedElementAsync(ViewModels.AddObjectViewModel addObjectViewModel);
        Task<NamedObjectSave> AddNewNamedObjectToAsync(ViewModels.AddObjectViewModel addObjectViewModel, GlueElement element, NamedObjectSave listToAddTo = null, bool selectNewNos = true);

        Task AddNamedObjectToAsync(NamedObjectSave newNos, GlueElement element, NamedObjectSave listToAddTo = null, bool selectNewNos = true,
            bool performSaveAndGenerateCode = true, bool updateUi = true);

        /// <summary>
        /// Assigns a variable on the argument NamedObject.
        /// </summary>
        /// <remarks>
        /// This also performs value conversion, updates the object according to its variablea ssignment, refreshes errors, and notifies plugins.
        /// </remarks>
        /// <example>
        /// To assign a value like X, the following code would be used:
        /// GlueCommands.Self.GluxCommands.SetVariableOn(namedObjectSave, "X", 100.0f);
        /// To assign a state value where the category is named CategoryName, the following code would be used:
        /// GlueCommands.Self.GluxCommands.SetVariableOn(namedObjectSave, "CurrentCategoryNameState", "NameOfStateAsString");
        /// </example>
        /// <param name="nos">The NamedObjectSave which will have its variable assigned.</param>
        /// <param name="memberName">The name of the variable to assign.</param>
        /// <param name="value">The value of the variable.</param>
        [Obsolete("Use SetVariableOnAsync")]
        void SetVariableOn(NamedObjectSave nos, string memberName, object value, bool performSaveAndGenerateCode = true,
            bool updateUi = true);

        Task SetVariableOnAsync(NamedObjectSave nos, string memberName, object value, bool performSaveAndGenerateCode = true,
            bool updateUi = true);

        Task SetVariableOnList(List<NosVariableAssignment> nosVariableAssignments,
            bool performSaveAndGenerateCode = true,
            bool updateUi = true);

        Task SetPropertyOnAsync(NamedObjectSave nos, string propertyName, object value, bool performSaveAndGenerateCode = true,
            bool updateUi = true);

        Task ReactToPropertyChanged(NamedObjectSave nos, string propertyName, object value, bool performSaveAndGenerateCode = true,
            bool updateUi = true);


        Task<List<ToolsUtilities.GeneralResponse<NamedObjectSave>>> CopyNamedObjectListIntoElement(List<NamedObjectSave> nosList, GlueElement targetElement, bool performSaveAndGenerateCode = true, bool updateUi = true);

        Task<ToolsUtilities.GeneralResponse<NamedObjectSave>> CopyNamedObjectIntoElement(NamedObjectSave nos, GlueElement targetElement, bool performSaveAndGenerateCode = true, bool updateUi = true);

        void RemoveNamedObject(NamedObjectSave namedObjectToRemove, bool performSaveAndGenerateCode = true, bool updateUi = true,
            List<string> additionalFilesToRemove = null);

        Task RemoveNamedObjectListAsync(List<NamedObjectSave> namedObjectListToRemove, bool performSaveAndGenerateCode = true,
            bool updateUi = true, List<string> additionalFilesToRemove = null);
        #endregion

        #region Custom Variable

        void RemoveCustomVariable(CustomVariable customVariable, List<string> additionalFilesToRemove = null);

        #endregion

        #region StateSaveCategory

        void RemoveStateSaveCategory(StateSaveCategory category);

        #endregion

        #region Import

        Task<GlueElement> ImportScreenOrEntityFromFile(FilePath filePath);

        #endregion
    }
}
