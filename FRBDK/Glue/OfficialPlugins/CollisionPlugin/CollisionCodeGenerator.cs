using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.CollisionPlugin
{
    public class CollisionCodeGenerator : ElementComponentCodeGenerator
    {
        public override ICodeBlock GenerateDestroy(ICodeBlock codeBlock, IElement element)
        {
			if (element is ScreenSave)
			{
				codeBlock.Line("FlatRedBall.Math.Collision.CollisionManager.Self.Relationships.Clear();");
			}
			return codeBlock;
        }
    }
}
