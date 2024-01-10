using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GumPlugin.Managers;
using GumPluginCore.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GumPluginCore.CodeGeneration
{
    class GumCollidableCodeGenerator : ElementComponentCodeGenerator
    {
        public override void AddInheritedTypesToList(List<string> listToAddTo, IElement element)
        {
            if(IsIGumCollidable(element))
            {
                listToAddTo.Add("GumCoreShared.FlatRedBall.Embedded.IGumCollidable");
            }
            base.AddInheritedTypesToList(listToAddTo, element);
        }

        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, IElement element)
        {
            if(IsIGumCollidable(element))
            {
                codeBlock.Line("public System.Collections.Generic.List<GumCoreShared.FlatRedBall.Embedded.GumToFrbShapeRelationship> GumToFrbShapeRelationships { get; set; } = new System.Collections.Generic.List<GumCoreShared.FlatRedBall.Embedded.GumToFrbShapeRelationship>();");
                codeBlock.Line("public System.Collections.Generic.List<GumCoreShared.FlatRedBall.Embedded.PositionedObjectGueWrapper> GumWrappers { get; set; }");
            }

            return base.GenerateFields(codeBlock, element);
        }

        public static void GenerateAddCollision(StringBuilder stringBuilder, IElement element, NamedObjectSave nos)
        {
            if(IsIGumCollidable(element) || InheritsFromIGumCollidable(element as GlueElement))
            {
                stringBuilder.AppendLine($"GumCoreShared.FlatRedBall.Embedded.GumCollidableExtensions.AddCollision(this, wrapperForAttachment);");
                stringBuilder.AppendLine($"GumCoreShared.FlatRedBall.Embedded.GumCollidableExtensions.UpdateShapePositionsFromGum(this);");
            }
        }

        public override ICodeBlock GenerateActivity(ICodeBlock codeBlock, IElement element)
        {
            if (IsIGumCollidable(element))
            {
                codeBlock.Line($"GumCoreShared.FlatRedBall.Embedded.GumCollidableExtensions.UpdateShapePositionsFromGum(this);");
            }
            return base.GenerateActivity(codeBlock, element);
        }

        public static bool IsIGumCollidable(IElement element) => 
            element is EntitySave entitySave && 
            entitySave.Properties.GetValue<bool>(GumCollidableManager.ImplementsIGumCollidable);

        public static bool InheritsFromIGumCollidable(GlueElement element)
        {
            return ObjectFinder.Self.GetAllBaseElementsRecursively(element).Any(item =>
                item.Properties.GetValue<bool>(GumCollidableManager.ImplementsIGumCollidable));
        }
    }
}
