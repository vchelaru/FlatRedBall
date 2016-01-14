using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.StandardTypes;
using FlatRedBall.Glue.Elements;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IElementCommands
    {
        ReferencedFileSave CreateReferencedFileSaveForExistingFile(IElement element, string directoryPath, string absoluteFileName, PromptHandleEnum unknownTypeHandle, 
            AssetTypeInfo ati,
            out string creationReport, out string errorMessage);

        SaveClasses.EntitySave AddEntity(string entityName);
        SaveClasses.EntitySave AddEntity(string entityName, bool is2D);

        void AddEntity(EntitySave entitySave);
        void AddEntity(EntitySave entitySave, bool suppressAlreadyExistingFileMessage);


    }
}
