using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPluginsCore.DamageDealingPlugin.CodeGenerators
{
    class DamageDealingCodeGenerator : ElementComponentCodeGenerator
    {
        public override void AddInheritedTypesToList(List<string> listToAddTo, IElement element)
        {
            if(element is EntitySave entity)
            {
                if(ImplementsIDamageArea(entity))
                {
                    listToAddTo.Add("FlatRedBall.Entities.IDamageArea");
                }
                if(ImplementsIDamageable(entity))
                {
                    listToAddTo.Add("FlatRedBall.Entities.IDamageable");
                }
            }
        }

        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, IElement element)
        {
            if(element is EntitySave entity)
            {
                if (ImplementsIDamageArea(entity))
                {
                    // these 2 are exposed in Glue:
                    //codeBlock.Line("public double SecondsBetweenDamage { get; set; }");
                    //codeBlock.Line("public int TeamIndex { get; set; }");

                    codeBlock.Line("public object DamageDealer { get; set; }");
                    codeBlock.Line("public event Action Destroyed;");
                }
                if (ImplementsIDamageable(entity))
                {
                    // This is exposed in Glue
                    //codeBlock.Line("public int TeamIndex { get; set; }");

                    codeBlock.Line("public System.Collections.Generic.Dictionary<FlatRedBall.Entities.IDamageArea, double> DamageAreaLastDamage { get; set; } = new System.Collections.Generic.Dictionary<FlatRedBall.Entities.IDamageArea, double>();");

                }
            }

            return codeBlock;
        }

        public override ICodeBlock GenerateDestroy(ICodeBlock codeBlock, IElement element)
        {
            if (element is EntitySave entity)
            {
                if (ImplementsIDamageArea(entity))
                {
                    codeBlock.Line("Destroyed?.Invoke();");

                    if (entity.CreatedByOtherEntities && entity.PooledByFactory)
                    {
                        codeBlock
                            .If("wasUsed")
                                .Line("Destroyed = null;");
                    }
                }
            }

            return codeBlock;
        }

        public static bool ImplementsIDamageArea(EntitySave entity)
        {
            return entity.Properties.GetValue<bool>("ImplementsIDamageArea");
        }

        public static bool ImplementsIDamageable(EntitySave entity)
        {
            return entity.Properties.GetValue<bool>("ImplementsIDamageable");
        }
    }
}
