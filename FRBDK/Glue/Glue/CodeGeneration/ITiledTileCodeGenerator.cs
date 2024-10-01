using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Instructions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.CodeGeneration {

    public class ITiledTileMetadataCodeGenerator: ElementComponentCodeGenerator {

        public override void AddInheritedTypesToList(List<string> listToAddTo, GlueElement element) {
            if(GlueState.Self.CurrentGlueProject.FileVersion < (int)FlatRedBall.Glue.SaveClasses.GlueProjectSave.GluxVersions.ITiledTileMetadataInFrb)
                return;

            if(element is EntitySave && ((EntitySave)element).ImplementsITiledTileMetadata) {
                listToAddTo.Add("FlatRedBall.Entities.ITiledTileMetadata");
            }
        }

    }

}

