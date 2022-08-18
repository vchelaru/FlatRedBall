using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.DoorEntityPlugin.CodeGenerators
{
    internal class DoorEntityCodeGenerator : ElementComponentCodeGenerator
    {


        bool IsDoorEntity(IElement element) => element is EntitySave && element.ClassName == "DoorEntity";

        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, IElement element)
        {
            if(IsDoorEntity(element))
            {
                codeBlock.Line("float FlatRedBall.Math.Geometry.IScalable.ScaleX { get => this.Width/2; set => Width = value * 2; }");
                codeBlock.Line("float FlatRedBall.Math.Geometry.IScalable.ScaleY { get => this.Height/2; set => Height = value * 2; }");
                codeBlock.Line("float FlatRedBall.Math.Geometry.IScalable.ScaleXVelocity { get => 0; set {} }");
                codeBlock.Line("float FlatRedBall.Math.Geometry.IScalable.ScaleYVelocity { get => 0; set {} }");

                codeBlock.Line("float FlatRedBall.Math.Geometry.IReadOnlyScalable.ScaleX => this.Width/2;");

                codeBlock.Line("float FlatRedBall.Math.Geometry.IReadOnlyScalable.ScaleY => this.Height/2;");

                codeBlock.Line("public static string NextDestinationObject { get; set; }");
                codeBlock.Line("public static float? NextDestinationX { get; set; }");
                codeBlock.Line("public static float? NextDestinationY { get; set; }");
            }
            return codeBlock;
        }

        public override ICodeBlock GenerateAdditionalMethods(ICodeBlock codeBlock, IElement element)
        {
            if(IsDoorEntity(element))
            {
                codeBlock.Line("bool isDoorNavigationInProgress;");
                var block = codeBlock.Function("public void", "DoNavigation");

                block.If("isDoorNavigationInProgress")
                    .Line("return;");

                // todo - need to add a navigation event
                block.Line("isDoorNavigationInProgress = true;");

                block = block.If("!string.IsNullOrEmpty(DestinationScreen)");
                block.Line("FlatRedBall.Screens.ScreenManager.CurrentScreen.MoveToScreen(DestinationScreen);");

                block.Line("NextDestinationObject = DestinationObject;");
                block.Line("NextDestinationX = DestinationX;");
                block.Line("NextDestinationY = DestinationY;");

                // this will be true then false immediately, but will matter when we eventually add a navigation transition
                block.Line("isDoorNavigationInProgress = false;");

            }

            return codeBlock;
        }

        public override void AddInheritedTypesToList(List<string> listToAddTo, IElement element)
        {
            if(IsDoorEntity(element))
            {
                listToAddTo.Add(typeof(IScalable).FullName);
            }
        }

        public override void GenerateEvent(ICodeBlock codeBlock, GlueElement element, EventResponseSave ers)
        {
            var potentialCollisionRelationship = element.GetNamedObjectRecursively(ers.SourceObject);

            ///////////////////Early Out/////////////////////
            if(potentialCollisionRelationship?.IsCollisionRelationship() != true)
            {
                return;
            }
            ////////////////End Early Out////////////////////

            var firstCollisionName = potentialCollisionRelationship.Properties.GetValue<string>("FirstCollisionName");
            var secondCollisionName = potentialCollisionRelationship.Properties.GetValue<string>("SecondCollisionName");

            var firstCollisionObject = element.GetNamedObjectRecursively(firstCollisionName);
            var secondCollisionObject = element.GetNamedObjectRecursively(secondCollisionName);

            string parameterName = null;
            // are either of these door lists?
            if (IsDoorEntityList(firstCollisionObject))
            {
                var args = ers.GetArgsForMethod(element);

                parameterName = args.Split(' ')[1];
            }
            
            else if (IsDoorEntityList(secondCollisionObject))
            {
                var args = ers.GetArgsForMethod(element);
                parameterName = args.Split(' ')[3];
            }

            if(parameterName != null)
            {
                var block = codeBlock.If($"{parameterName}.AutoNavigate");
                block.Line($"{parameterName}.DoNavigation();");
            }

            base.GenerateEvent(codeBlock, element, ers);
        }

        private bool IsDoorEntityList(NamedObjectSave firstCollisionObject)
        {
            return
                firstCollisionObject != null &&
                firstCollisionObject.IsList &&
                !string.IsNullOrEmpty(firstCollisionObject.SourceClassGenericType) &&
                AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(firstCollisionObject.SourceClassGenericType, null)?.FriendlyName == "DoorEntity";

        }
    }
}
