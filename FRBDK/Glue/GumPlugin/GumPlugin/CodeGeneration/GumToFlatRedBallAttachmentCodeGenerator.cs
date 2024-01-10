using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GumPlugin.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumPlugin.CodeGeneration
{
    class GumToFlatRedBallAttachmentCodeGenerator : ElementComponentCodeGenerator
    {
        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, IElement element)
        {
            if(GetIfHasAttachedGumInstances(element as GlueElement))
            {
                codeBlock.Line(
                    "System.Collections.Generic.List<GumCoreShared.FlatRedBall.Embedded.PositionedObjectGueWrapper> gumAttachmentWrappers = new System.Collections.Generic.List<GumCoreShared.FlatRedBall.Embedded.PositionedObjectGueWrapper>();");
            }
            return base.GenerateFields(codeBlock, element);
        }

        public override ICodeBlock GenerateDestroy(ICodeBlock codeBlock, IElement element)
        {
            if (GetIfHasAttachedGumInstances(element as GlueElement))
            {
                codeBlock.For("int i = gumAttachmentWrappers.Count-1; i > -1; i--")
                    .Line("FlatRedBall.SpriteManager.RemovePositionedObject(gumAttachmentWrappers[i]);");
            }
            return base.GenerateDestroy(codeBlock, element);
        }

        bool GetIfHasAttachedGumInstances(GlueElement element)
        {
            if(element is EntitySave)
            {
                return element.GetAllNamedObjectsRecurisvely()
                    .Any(item => AssetTypeInfoManager.Self.IsAssetTypeInfoGum(item.GetAssetTypeInfo()));
            }

            return false;
        }
    }
}
    