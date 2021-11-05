using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using GlueFormsCore.ViewModels;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IElementCommands
    {
        ReferencedFileSave CreateReferencedFileSaveForExistingFile(IElement element, string directoryPath, string absoluteFileName,
            StandardTypes.PromptHandleEnum unknownTypeHandle,
            Elements.AssetTypeInfo ati,
            out string creationReport, out string errorMessage);

        SaveClasses.EntitySave AddEntity(string entityName, bool is2D = false);

        SaveClasses.EntitySave AddEntity(AddEntityViewModel viewModel, string directory = null);

        void AddEntity(EntitySave entitySave);
        void AddEntity(EntitySave entitySave, bool suppressAlreadyExistingFileMessage);
        bool UpdateFromBaseType(GlueElement glueElement);

        void AddCustomVariableToCurrentElement(CustomVariable newVariable, bool save = true);
    }
}
