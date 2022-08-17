using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.SaveClasses;
using GlueFormsCore.ViewModels;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
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

        Task RenameElement(GlueElement elementToRename, string value);

        #endregion

        #region Add CustomVariable
        Task AddStateCategoryCustomVariableToElementAsync(StateSaveCategory category, GlueElement element, bool save = true);


        void AddCustomVariableToCurrentElement(CustomVariable newVariable, bool save = true);
        void AddCustomVariableToElement(CustomVariable newVariable, GlueElement element, bool save = true);
        Task AddCustomVariableToElementAsync(CustomVariable newVariable, GlueElement element, bool save = true);
        #endregion

        #region Events

        Task AddEventToElement(AddEventViewModel viewModel, GlueElement glueElement);

        void AddEventToElement(GlueElement currentElement, EventResponseSave eventResponseSave);

        #endregion

        bool UpdateFromBaseType(GlueElement glueElement);

    }
}
