using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.CollisionPlugin.Managers;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.CollisionPlugin.CodeGenerators
{
    internal class StackableCodeGenerator : ElementComponentCodeGenerator
    {
        public override void AddInheritedTypesToList(List<string> listToAddTo, IElement element)
        {
            if (StackableEntityManager.Self.ImplementsIStackable(element as GlueElement))
            {
                listToAddTo.Add("FlatRedBall.Math.Geometry.IStackable");
            }
        }
        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, IElement element)
        {
            if (StackableEntityManager.Self.ImplementsIStackable(element as GlueElement))
            {
                codeBlock.Line("public List<Microsoft.Xna.Framework.Vector3> LockVectors { get; set; } = new List<Microsoft.Xna.Framework.Vector3>();");
                codeBlock.Line("public List<Microsoft.Xna.Framework.Vector3> LockVectorsTemp { get; set; } = new List<Microsoft.Xna.Framework.Vector3>();");
            }
            return codeBlock;
        }

        public override ICodeBlock GenerateActivity(ICodeBlock codeBlock, IElement element)
        {
            if(StackableEntityManager.Self.ImplementsIStackable(element as GlueElement))
            {
                codeBlock.Line("LockVectors.Clear();");
                codeBlock.Line("LockVectors.AddRange(LockVectorsTemp);");
                codeBlock.Line("LockVectorsTemp.Clear();");
            }

            return codeBlock;
        }
    }
}
