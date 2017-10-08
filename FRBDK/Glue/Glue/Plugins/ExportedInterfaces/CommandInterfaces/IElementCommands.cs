using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IElementCommands
    {
#if GLUE
        ReferencedFileSave CreateReferencedFileSaveForExistingFile(IElement element, string directoryPath, string absoluteFileName,
            StandardTypes.PromptHandleEnum unknownTypeHandle,
            Elements.AssetTypeInfo ati,
            out string creationReport, out string errorMessage);
#endif
        SaveClasses.EntitySave AddEntity(string entityName);
        SaveClasses.EntitySave AddEntity(string entityName, bool is2D);

        void AddEntity(EntitySave entitySave);
        void AddEntity(EntitySave entitySave, bool suppressAlreadyExistingFileMessage);


    }
}
