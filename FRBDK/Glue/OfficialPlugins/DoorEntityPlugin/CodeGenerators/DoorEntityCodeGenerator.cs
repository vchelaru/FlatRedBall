using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
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
    }
}
