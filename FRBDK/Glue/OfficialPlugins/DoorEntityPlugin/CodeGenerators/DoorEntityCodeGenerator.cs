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

            // are either of these door lists?
            if(IsDoorEntityList(firstCollisionObject) || IsDoorEntityList(secondCollisionObject))
            {

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
