using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;

namespace FlatRedBall.Glue.CodeGeneration
{
    internal class PooledCodeGenerator : ElementComponentCodeGenerator
    {
        public override void AddInheritedTypesToList(List<string> listToAddTo, IElement element)
        {
            if (element is EntitySave && ((EntitySave)element).CreatedByOtherEntities)
            {
                listToAddTo.Add("FlatRedBall.Performance.IPoolable");
            }
        }

        public override CodeBuilder.ICodeBlock GenerateFields(CodeBuilder.ICodeBlock codeBlock, SaveClasses.IElement element)
        {

            if (element is EntitySave)
            {
                EntitySave asEntity = element as EntitySave;

                if (asEntity.CreatedByOtherEntities && asEntity.GetAllBaseEntities().Count(item=>item.CreatedByOtherEntities) == 0)
                {
                    codeBlock.AutoProperty("public int", "Index");
                    codeBlock.AutoProperty("public bool", "Used");
                }
            }

            return codeBlock;
        }
    }
}
