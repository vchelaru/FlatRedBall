using FlatRedBall.Entities;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ICollidablePlugins;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using OfficialPluginsCore.DamageDealingPlugin.CodeGenerators;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace OfficialPluginsCore.DamageDealingPlugin
{
    [Export(typeof(PluginBase))]
    public class MainDamageDealingPlugin : PluginBase
    {
        public override string FriendlyName => "Damage Dealing Plugin";

        public override Version Version => new Version(1,0);

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            this.AdjustDisplayedEntity += HandleDisplayedEntity;
            RegisterCodeGenerator(new DamageDealingCodeGenerator());

            this.ReactToChangedPropertyHandler += HandleChangedProperty;
            this.ReactToVariableRemoved += HandleVariableRemoved;
            //this.AdjustDisplayedNamedObject += HandleAdjustNamedObjectDisplayed;

            //this.GetVariableDefinitionsForElement += HandleGetVariableDefinitionsForElement;
        }

        private void HandleVariableRemoved(CustomVariable variable)
        {
            var currentEntity = GlueState.Self.CurrentEntitySave;
            if(currentEntity == null)
            {
                return;
            }

            var implementsDamageArea = DamageDealingCodeGenerator.ImplementsIDamageArea(currentEntity);
            var implementsDamageable = DamageDealingCodeGenerator.ImplementsIDamageable(currentEntity);

            var shouldReadd = false;

            shouldReadd =
                variable.Name == nameof(IDamageable.TeamIndex) && (implementsDamageArea || implementsDamageable);

            var isV2 = DamageDealingCodeGenerator.UsesDamageV2;

            if( implementsDamageArea && !shouldReadd)
            {
                shouldReadd =
                    variable.Name == nameof(IDamageArea.SecondsBetweenDamage) ||
                    (isV2 && variable.Name == nameof(IDamageArea.DamageToDeal));
            }

            if(implementsDamageable && !shouldReadd)
            {
                shouldReadd =
                    (isV2 && variable.Name == nameof(IDamageable.MaxHealth));
            }

            if (shouldReadd)
            {
                currentEntity.CustomVariables.Add(variable);
                // readd it, notify
                GlueCommands.Self.PrintError($"Cannot remove variable because it is required by interface - readding {variable}");
            }


        }

        static CustomVariable GetTeamIndex(bool isV2)
        {
            var variableDefinition = new CustomVariable();
            variableDefinition.Name = nameof(IDamageable.TeamIndex);
            variableDefinition.DefaultValue = 0;
            variableDefinition.Type = "int";
            variableDefinition.CreatesProperty = true;
            //variableDefinition.Category = "Damage Dealing";
            return variableDefinition;
        }
        static CustomVariable GetMaxHealth(bool isV2)
        {
            if(isV2)
            {
                var variable = new CustomVariable();
                variable.Name = nameof(IDamageable.MaxHealth);
                variable.DefaultValue = 100m;
                variable.Type = "decimal";
                variable.CreatesProperty = true;
                // why no category?
                return variable;
            }
            else
            {
                return null;
            }
        }

        static CustomVariable GetSecondsBetweenDamage(bool isV2)
        {
            var secondsBetweenDamage = new CustomVariable();
            secondsBetweenDamage.Name = nameof(IDamageArea.SecondsBetweenDamage);
            secondsBetweenDamage.DefaultValue = 0.0;
            secondsBetweenDamage.Type = "double";
            secondsBetweenDamage.CreatesProperty = true;
            //secondsBetweenDamage.Category = "Damage Dealing";
            return secondsBetweenDamage;
        }

        static CustomVariable GetDamageToDeal(bool isV2)
        {
            if(isV2)
            {
                var variable = new CustomVariable();
                variable.Name = nameof(IDamageArea.DamageToDeal);
                variable.DefaultValue = 10m;
                variable.Type = "decimal";
                variable.CreatesProperty = true;
                // why no category?
                return variable;
            }
            else
            {
                return null;
            }
        }

        List<Func<bool, CustomVariable>> IDamageAreaVariables = new List<Func<bool, CustomVariable>>
        {
            GetTeamIndex,
            GetSecondsBetweenDamage,
            GetDamageToDeal
        };
        List<Func<bool, CustomVariable>> IDamageableVariables = new List<Func<bool, CustomVariable>>
        {
            GetTeamIndex,
            GetMaxHealth
        };


        private void HandleChangedProperty(string changedMember, object oldValue, GlueElement glueElement)
        {

            var wereVariablesAddedOrRemoved = false;

            var currentEntity = glueElement as EntitySave;

            if (currentEntity != null)
            {
                var teamIndexVariable = currentEntity.GetCustomVariable(nameof(IDamageable.TeamIndex));
                var secondsBetweenDamage = currentEntity.GetCustomVariable(nameof(IDamageArea.SecondsBetweenDamage));

                var isV2 = DamageDealingCodeGenerator.UsesDamageV2;

                if (changedMember == ImplementsIDamageArea)
                {
                    List<CustomVariable> variables = IDamageAreaVariables.Select(item => item(isV2)).ToList();
                    var implements = DamageDealingCodeGenerator.ImplementsIDamageArea(currentEntity);

                    wereVariablesAddedOrRemoved = RespondToImplementationChanged(currentEntity, variables, implements);
                }
                else if(changedMember == ImplementsIDamageable)
                {
                    var variables = IDamageableVariables.Select(item => item(isV2)).ToList();
                    var implements = DamageDealingCodeGenerator.ImplementsIDamageable(currentEntity);

                    wereVariablesAddedOrRemoved = RespondToImplementationChanged(currentEntity, variables, implements);
                }

                if(wereVariablesAddedOrRemoved)
                {
                    GlueCommands.Self.GluxCommands.SaveGlux();
                    GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
                    GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();
                    GlueCommands.Self.RefreshCommands.RefreshVariables();
                }
            }
        }

        private static bool RespondToImplementationChanged(EntitySave currentEntity, List<CustomVariable> variables, bool implements)
        {
            bool wereVariablesAddedOrRemoved = false;
            if (implements)
            {
                foreach (var variable in variables.Where(item => item != null))
                {
                    var existing = currentEntity.GetCustomVariable(variable.Name);

                    if (existing == null)
                    {
                        currentEntity.CustomVariables.Add(variable);
                        wereVariablesAddedOrRemoved = true;
                    }
                }
            }
            else
            {
                foreach (var variable in variables.Where(item => item != null))
                {
                    var existing = currentEntity.GetCustomVariable(variable.Name);

                    if (existing != null)
                    {
                        currentEntity.CustomVariables.Remove(existing);
                        wereVariablesAddedOrRemoved = true;
                    }
                }
            }

            return wereVariablesAddedOrRemoved;
        }

        public const string ImplementsIDamageArea = nameof(ImplementsIDamageArea);
        public const string ImplementsIDamageable = nameof(ImplementsIDamageable);
        public const string SuppressDamagePropertyCodeGeneration = nameof(SuppressDamagePropertyCodeGeneration);

        internal static void HandleDisplayedEntity(EntitySave entitySave, EntitySavePropertyGridDisplayer displayer)
        {
            // Only show this if this is an ICollidable
            if (entitySave.IsICollidableRecursive())
            {
                var member = displayer.IncludeCustomPropertyMember(ImplementsIDamageArea, typeof(bool));

                member.SetCategory("Damage");
            }

            var damageableMember = displayer.IncludeCustomPropertyMember(ImplementsIDamageable, typeof(bool));
            damageableMember.SetCategory("Damage");

            if(DamageDealingCodeGenerator.ImplementsIDamageArea(entitySave) ||
                DamageDealingCodeGenerator.ImplementsIDamageable(entitySave))
            {
                var suppressCodeGenMember = displayer.IncludeCustomPropertyMember(SuppressDamagePropertyCodeGeneration, typeof(bool));
                suppressCodeGenMember.SetCategory("Damage");
            }
        }
    }
}
