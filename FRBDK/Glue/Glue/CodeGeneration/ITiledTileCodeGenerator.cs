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

        public override void AddInheritedTypesToList(List<string> listToAddTo, IElement element) {
            if(GlueState.Self.CurrentGlueProject.FileVersion < (int)FlatRedBall.Glue.SaveClasses.GlueProjectSave.GluxVersions.ITiledTileMetadataInFrb)
                return;

            if(element is EntitySave && ((EntitySave)element).ImplementsITiledTileMetadata) {
                listToAddTo.Add("FlatRedBall.Entities.ITiledTileMetadata");
            }
        }

        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, IElement element) {
            if(GlueState.Self.CurrentGlueProject.FileVersion < (int)FlatRedBall.Glue.SaveClasses.GlueProjectSave.GluxVersions.ITiledTileMetadataInFrb)
                return codeBlock;

            EntitySave entitySave = element as EntitySave;

            if(entitySave != null && entitySave.ImplementsITiledTileMetadata) {
                bool inheritesFromITiledTileMetadata = entitySave.GetInheritsFromITiledTileMetadata();

                codeBlock.AutoProperty("public int", "TileLeftTexturePixel");
                codeBlock.AutoProperty("public int", "TileTopTexturePixel");
                codeBlock.AutoProperty("public int", "TileTexturePixelSize");
            }

            return codeBlock;
        }
    }

}

