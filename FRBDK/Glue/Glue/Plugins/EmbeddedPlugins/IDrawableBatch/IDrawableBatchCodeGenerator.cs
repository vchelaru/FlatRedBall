using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.CodeGeneration;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.IDrawableBatch
{
    public class IDrawableBatchCodeGenerator : ElementComponentCodeGenerator
    {

        bool ShouldGenerate(GlueElement element)
        {
            return element is EntitySave && ((EntitySave)element).ImplementsIDrawableBatch;
        }

        public override void AddInheritedTypesToList(List<string> listToAddTo, GlueElement element)
        {
            if (ShouldGenerate(element))
            {
                listToAddTo.Add("FlatRedBall.Graphics.IDrawableBatch");
            }
        }

        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, GlueElement element)
        {
            if (ShouldGenerate(element))
            {
                var entitySave = element as EntitySave;

                // The following are already handled by the Entity:
                // X, Y, Z
                codeBlock.Property("bool", "FlatRedBall.Graphics.IDrawableBatch.UpdateEveryFrame")
                    .Get()
                    .Line("return false;");
            }

            return codeBlock;
        }

        public override ICodeBlock GenerateAddToManagers(ICodeBlock codeBlock, GlueElement element)
        {
            if (ShouldGenerate(element))
            {
                var entity = element as EntitySave;

                codeBlock.Line(
                    "FlatRedBall.SpriteManager.AddToLayer((FlatRedBall.Graphics.IDrawableBatch)this, layerToAddTo);");
            }
            return codeBlock;
        }

        public override void GenerateRemoveFromManagers(ICodeBlock codeBlock, GlueElement element)
        {
            if(ShouldGenerate(element))
            {
                codeBlock.Line(
                    "FlatRedBall.SpriteManager.RemoveDrawableBatch(this);");
            }

        }

        public override ICodeBlock GenerateDestroy(ICodeBlock codeBlock, GlueElement element)
        {
            if (ShouldGenerate(element))
            {
                codeBlock.Line(
                    "FlatRedBall.SpriteManager.RemoveDrawableBatch(this);");
            }

            return codeBlock;

        }

        public override ICodeBlock GenerateAdditionalMethods(ICodeBlock codeBlock, GlueElement element)
        {
            if (element is EntitySave && ((EntitySave)element).ImplementsIDrawableBatch)
            {
                var entitySave = element as EntitySave;

                codeBlock.Line("void FlatRedBall.Graphics.IDrawableBatch.Update(){}");
                codeBlock.Line("void FlatRedBall.Graphics.IDrawableBatch.Destroy(){}");

            }

            return codeBlock;
        }
    }
}
