using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SetVariable;
using GlueFormsCore.ViewModels;
using ToolsUtilities;
using static FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces.ElementCommands;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public class OrderedNamedObjectPair
    {
        public NamedObjectSave First;
        public NamedObjectSave Second;
    }

    public interface IElementCommands
    {
        #region AddEntity

        SaveClasses.EntitySave AddEntity(string entityName, bool is2D = false, bool notifyPluginsOfNewEntity = true);

        Task<SaveClasses.EntitySave> AddEntityAsync(AddEntityViewModel viewModel);

        void AddEntity(EntitySave entitySave, bool suppressAlreadyExistingFileMessage = false, bool notifyPluginsOfNewEntity = true);

        #endregion

        #region ReferencedFile

        ReferencedFileSave CreateReferencedFileSaveForExistingFile(IElement element, string directoryPath, string absoluteFileName,
            StandardTypes.PromptHandleEnum unknownTypeHandle,
            Elements.AssetTypeInfo ati,
            out string creationReport, out string errorMessage);

        #endregion

        #region StateSaveCategory

        Task AddStateSaveCategoryAsync(string categoryName, GlueElement element);

        #endregion

        #region GlueElement

        /// <summary>
        /// Performs all logic related to renaming an element. This should get called
        /// before the element's name has been set. This will internally set the element's name.
        /// The fullNewName is the full name of the element such as "Screens\\GameScreen".
        /// </summary>
        /// <param name="elementToRename">The element to rename.</param>
        /// <param name="fullNewName">The full name including prefixes. For example, 
        /// "Entities\\Subfolder\\NewName".</param>
        /// <param name="showRenameWindow">Whether to show a window that provides a summary of changes made during the remove</param>
        /// <returns>A task which completes when all logic and UI are finished.</returns>
        Task RenameElement(GlueElement elementToRename, string fullNewName, bool showRenameWindow = true);

        #endregion

        #region Add CustomVariable
        Task<GeneralResponse> AddStateCategoryCustomVariableToElementAsync(StateSaveCategory category, GlueElement element, bool save = true);


        void AddCustomVariableToCurrentElement(CustomVariable newVariable, bool save = true);
        
        Task AddCustomVariableToElementAsync(CustomVariable newVariable, GlueElement element, bool save = true);
        #endregion

        #region Set CustomVariable

        Task HandleSetVariable(CustomVariable variable, object value, bool performSaveAndGenerateCode = true,
            bool updateUi = true);

        #endregion

        #region Events

        Task AddEventToElement(AddEventViewModel viewModel, GlueElement glueElement);

        void AddEventToElement(GlueElement currentElement, EventResponseSave eventResponseSave);

        #endregion

        #region Property Set

        Task ReactToPropertyChanged(GlueElement element, string propertyName, object oldValue);

        #endregion

        List<OrderedNamedObjectPair> GetGameScreenOpposingTeamIndexCollisionPairs(int? newTeamIndex, NamedObjectSave newElementList, AddEntityViewModel viewModel);

        #region Inheritance

        bool UpdateFromBaseType(GlueElement glueElement, bool showPopupAboutObjectErrors = true);

        #endregion

    }
}
