using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.CodeGeneration.Game1
{
    public class Game1CodeGenerator
    {

        public virtual CodeLocation CodeLocation { get; set; } = CodeLocation.StandardGenerated;

        public virtual void GenerateClassScope(ICodeBlock codeBlock)
        {

        }

        public virtual void GenerateInitializeEarly(ICodeBlock codeBlock) { }

        public virtual void GenerateInitialize(ICodeBlock codeBlock) { }

        public virtual void GenerateUpdate(ICodeBlock codeBlock) { }

        public virtual void GenerateDrawEarly(ICodeBlock codeBlock) { }

        public virtual void GenerateDraw(ICodeBlock codeBlock)
        {

        }
    }
}
