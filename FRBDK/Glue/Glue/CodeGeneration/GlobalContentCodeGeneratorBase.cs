using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.CodeGeneration
{
    public class GlobalContentCodeGeneratorBase
    {
        // We use InitializeStart and InitializeEnd to control code location
        //public virtual CodeLocation CodeLocation
        //{
        //    get
        //    {
        //        return Plugins.Interfaces.CodeLocation.StandardGenerated;
        //    }
        //}

        public virtual void GenerateInitializeStart(ICodeBlock codeBlock)
        {

        }


        public virtual void GenerateInitializeEnd(ICodeBlock codeBlock)
        {

        }

        public virtual void GenerateAdditionalMethods(ICodeBlock codeBlock)
        {

        }
    }
}
