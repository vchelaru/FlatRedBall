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
        bool IsPoolable(IElement element)
        {
            return element is EntitySave asEntity &&
                asEntity.PooledByFactory &&
                asEntity.CreatedByOtherEntities &&
                asEntity.GetAllBaseEntities().Count(item => IsPoolable(item)) == 0;
        }


        public override void AddInheritedTypesToList(List<string> listToAddTo, IElement element)
        {
            if (IsPoolable(element))
            {
                listToAddTo.Add("FlatRedBall.Performance.IPoolable");
            }
        }

        public override CodeBuilder.ICodeBlock GenerateFields(CodeBuilder.ICodeBlock codeBlock, SaveClasses.IElement element)
        {

            if (element is EntitySave)
            {
                EntitySave asEntity = element as EntitySave;

                // Sept 19, 2022
                // Shouldn't this only be true if pooled?
                //if (asEntity.CreatedByOtherEntities && asEntity.GetAllBaseEntities().Count(item=>item.CreatedByOtherEntities) == 0)
                if (IsPoolable(element))
                {
                    codeBlock.AutoProperty("public int", "Index");
                    codeBlock.AutoProperty("public bool", "Used");
                }
            }

            return codeBlock;
        }
    }
}
