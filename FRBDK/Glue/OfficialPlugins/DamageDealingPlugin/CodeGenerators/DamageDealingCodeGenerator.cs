using FlatRedBall.Entities;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
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
            if (element is EntitySave entity)
            {
                if (ImplementsIDamageArea(entity))
                {
                    listToAddTo.Add("FlatRedBall.Entities.IDamageArea");
                }
                if (ImplementsIDamageable(entity))
                {
                    listToAddTo.Add("FlatRedBall.Entities.IDamageable");
                }
            }
        }

        #endregion

        public static bool UsesDamageV2 => GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.DamageableHasHealth;

        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, IElement element)
        {
            if (element is EntitySave entity)
            {
                var shouldImplementIDamageArea =
                    ImplementsIDamageArea(entity) && !SuppressDamagePropertyCodeGeneration(entity);
                var shouldImplementIDamageable =
                    ImplementsIDamageable(entity) && !SuppressDamagePropertyCodeGeneration(entity);

                // Explicit is necessary if the entity implements both
                // IDamageable and IDamageArea because that means it will
                // have events that are named the same.

                if (shouldImplementIDamageArea)
                {
                    // these variables are exposed in Glue, not pure codegen, so the user can modify them:
                    //codeBlock.Line("public double SecondsBetweenDamage { get; set; }");
                    //codeBlock.Line("public int TeamIndex { get; set; }");
                    //codeBlock.Line("public decimal DamageToDeal { get; set; }");

                    codeBlock.Line("public Action<decimal, FlatRedBall.Entities.IDamageable> ReactToDamageDealt { get; set; }");
                    codeBlock.Line("public Func<decimal, FlatRedBall.Entities.IDamageable, decimal> ModifyDamageDealt { get; set; }");


                    codeBlock.Line("public object DamageDealer { get; set; }");
                    codeBlock.Line("public event Action Destroyed;");

                    if (UsesDamageV2)
                    {
                        // See note about explicit implementation above
                        var shouldBeExplicit = shouldImplementIDamageable && shouldImplementIDamageArea;


                        codeBlock.Line("public Action<decimal, FlatRedBall.Entities.IDamageable> KilledDamageable { get; set; }");
                        codeBlock.Line("public Action<FlatRedBall.Entities.IDamageable> RemovedByCollision { get; set; }");

                    }
                }

                if (shouldImplementIDamageable)
                {
                    // This is exposed in Glue
                    //codeBlock.Line("public int TeamIndex { get; set; }");

                    codeBlock.Line("public System.Collections.Generic.Dictionary<FlatRedBall.Entities.IDamageArea, double> DamageAreaLastDamage { get; set; } = new System.Collections.Generic.Dictionary<FlatRedBall.Entities.IDamageArea, double>();");

                    if (UsesDamageV2)
                    {
                        // See note about explicit implementation above
                        var shouldBeExplicit = shouldImplementIDamageable && shouldImplementIDamageArea;

                        codeBlock.Line("public Action<decimal, FlatRedBall.Entities.IDamageArea> ReactToDamageReceived { get; set; }");
                        codeBlock.Line("public Func<decimal, FlatRedBall.Entities.IDamageArea, decimal> ModifyDamageReceived { get; set; }");

                        codeBlock.Line("public decimal CurrentHealth { get; set; }");
                        codeBlock.Line("public Action<decimal, FlatRedBall.Entities.IDamageArea> Died { get; set; }");
                    }
                }
            }

            return codeBlock;
        }

        public override ICodeBlock GenerateInitialize(ICodeBlock codeBlock, IElement element)
        {
            // Can't do this here, because derived needs a chance to set this
            // Moving to AddToManagers
            //if (UsesDamageV2 && ImplementsIDamageable(element as EntitySave))
            //{
            //    codeBlock.Line("CurrentHealth = MaxHealth;");
            //}

            return base.GenerateInitialize(codeBlock, element);
        }

        public override void GenerateAddToManagersBottomUp(ICodeBlock codeBlock, IElement element)
        {
            if (UsesDamageV2 && ImplementsIDamageableRecursively(element as EntitySave))
            {
                codeBlock.Line("CurrentHealth = MaxHealth;");
            }
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

        public static bool ImplementsIDamageableRecursively(EntitySave entity)
        {
            if(entity?.Properties.GetValue<bool>(MainDamageDealingPlugin.ImplementsIDamageable) == true)
            {
                return true;
            }
            else
            {
                var baseEntity = ObjectFinder.Self.GetBaseElement(entity) as EntitySave;
                if(baseEntity != null)
                {
                    return ImplementsIDamageableRecursively(baseEntity);
                }
            }
            return false;
        }


        public static bool SuppressDamagePropertyCodeGeneration(EntitySave entity) =>
            entity.Properties.GetValue<bool>(MainDamageDealingPlugin.SuppressDamagePropertyCodeGeneration);

    }
}
