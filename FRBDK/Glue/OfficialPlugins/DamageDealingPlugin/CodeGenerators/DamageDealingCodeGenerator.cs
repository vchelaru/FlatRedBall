using FlatRedBall.Entities;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPluginsCore.DamageDealingPlugin.CodeGenerators
{
    class DamageDealingCodeGenerator : ElementComponentCodeGenerator
    {
        #region Inheritance

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

        #endregion

        public static bool UsesDamageV2 => GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.DamageableHasHealth;

        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, IElement element)
        {
            if(element is EntitySave entity)
            {
                if (ImplementsIDamageArea(entity) && !SuppressDamagePropertyCodeGeneration(entity))
                {
                    // these 2 are exposed in Glue:
                    //codeBlock.Line("public double SecondsBetweenDamage { get; set; }");
                    //codeBlock.Line("public int TeamIndex { get; set; }");
                    //codeBlock.Line("public decimal DamageToDeal { get; set; }");

                    codeBlock.Line("public object DamageDealer { get; set; }");
                    codeBlock.Line("public event Action Destroyed;");

                    if(UsesDamageV2)
                    {
                        codeBlock.Line("public Func<decimal, FlatRedBall.Entities.IDamageable, decimal> ModifyDamageDealt { get; set; }");
                        codeBlock.Line("public Action<decimal, FlatRedBall.Entities.IDamageable> ReactToDamageDealt { get; set; }");
                        codeBlock.Line("public Action<decimal, FlatRedBall.Entities.IDamageable> KilledDamageable { get; set; }");
    }
                }
                if (ImplementsIDamageable(entity) && !SuppressDamagePropertyCodeGeneration(entity))
                {
                    // This is exposed in Glue
                    //codeBlock.Line("public int TeamIndex { get; set; }");

                    codeBlock.Line("public System.Collections.Generic.Dictionary<FlatRedBall.Entities.IDamageArea, double> DamageAreaLastDamage { get; set; } = new System.Collections.Generic.Dictionary<FlatRedBall.Entities.IDamageArea, double>();");

                    if (UsesDamageV2)
                    {
                        codeBlock.Line("public decimal CurrentHealth { get; set; }");
                        codeBlock.Line("public Func<decimal, FlatRedBall.Entities.IDamageArea, decimal> ModifyDamageDealt { get; set; }");
                        codeBlock.Line("public Action<decimal, FlatRedBall.Entities.IDamageArea> ReactToDamageDealt { get; set; }");
                        codeBlock.Line("public Action<decimal, FlatRedBall.Entities.IDamageArea> Died { get; set; }");
    }
                }
            }

            return codeBlock;
        }

        public override ICodeBlock GenerateInitialize(ICodeBlock codeBlock, IElement element)
        {
            if(UsesDamageV2 && ImplementsIDamageable(element as EntitySave))
            {
                codeBlock.Line("CurrentHealth = MaxHealth;");
            }

            return base.GenerateInitialize(codeBlock, element);
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

        public static bool ImplementsIDamageArea(EntitySave entity) =>
            entity?.Properties.GetValue<bool>(MainDamageDealingPlugin.ImplementsIDamageArea) == true;

        public static bool ImplementsIDamageable(EntitySave entity) => 
            entity?.Properties.GetValue<bool>(MainDamageDealingPlugin.ImplementsIDamageable) == true;

        public static bool SuppressDamagePropertyCodeGeneration(EntitySave entity) =>
            entity.Properties.GetValue<bool>(MainDamageDealingPlugin.SuppressDamagePropertyCodeGeneration);

    }
}
