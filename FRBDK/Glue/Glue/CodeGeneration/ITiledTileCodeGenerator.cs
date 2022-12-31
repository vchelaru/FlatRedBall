using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
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
            if(element is EntitySave && ((EntitySave)element).ImplementsITiledTileMetadata) {
                listToAddTo.Add("FlatRedBall.Entities.ITiledTileMetadata");
            }
        }

        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, IElement element) {
            EntitySave entitySave = element as EntitySave;

            if(entitySave != null && entitySave.ImplementsITiledTileMetadata) {
                bool inheritesFromITiledTileMetadata = entitySave.GetInheritsFromITiledTileMetadata();

                codeBlock.Line("//partial void Tile_TexturePixelsSet();");
                codeBlock.AutoProperty("public int", "Tile_LeftTexturePixel");
                codeBlock.AutoProperty("public int", "Tile_TopTexturePixel");
                codeBlock.AutoProperty("public int", "Tile_TexturePixelSize");
            }

            return codeBlock;
        }
    }

}

