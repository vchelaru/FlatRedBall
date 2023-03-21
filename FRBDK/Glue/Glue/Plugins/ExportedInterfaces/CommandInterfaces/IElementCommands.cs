using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SetVariable;
using GlueFormsCore.ViewModels;
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

        SaveClasses.EntitySave AddEntity(string entityName, bool is2D = false);

        Task<SaveClasses.EntitySave> AddEntityAsync(AddEntityViewModel viewModel, string directory = null);

        void AddEntity(EntitySave entitySave);
        void AddEntity(EntitySave entitySave, bool suppressAlreadyExistingFileMessage);


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
        /// Performs all logic related to renaming an element. The name should not have the "Screens\\" or "Entities\\" prefix, nor any prefixes
        /// for the entity's folder. In other words, GameScreen would be "GameScreen" rather than "Screens\\GameScreen".
        /// </summary>
        /// <param name="elementToRename">The element to rename.</param>
        /// <param name="value">The new name without any prefixes. For example, even an entity in a folder should pass "NewName" rather than 
        /// "Entities\\Subfolder\\NewName".</param>
        /// <returns>A task which completes when all logic and UI are finished.</returns>
        Task RenameElement(GlueElement elementToRename, string value);

        #endregion

        #region Add CustomVariable
        Task AddStateCategoryCustomVariableToElementAsync(StateSaveCategory category, GlueElement element, bool save = true);


        void AddCustomVariableToCurrentElement(CustomVariable newVariable, bool save = true);
        [Obsolete("Use AddCustomVariableToElementAsync")]
        void AddCustomVariableToElement(CustomVariable newVariable, GlueElement element, bool save = true);
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


        bool UpdateFromBaseType(GlueElement glueElement);

    }
}
