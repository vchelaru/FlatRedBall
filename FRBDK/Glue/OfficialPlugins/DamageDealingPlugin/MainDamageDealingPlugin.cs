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

            var shouldReadd =
                variable.Name == nameof(IDamageable.TeamIndex) && (implementsDamageArea || implementsDamageable);

            if(!shouldReadd)
            {
                shouldReadd =
                    variable.Name == nameof(IDamageArea.SecondsBetweenDamage) && implementsDamageArea;
            }
            if (shouldReadd)
            {
                currentEntity.CustomVariables.Add(variable);
                // readd it, notify
                GlueCommands.Self.PrintError($"Cannot remove variable because it is required by interface - readding {variable}");
            }


        }

        private void HandleChangedProperty(string changedMember, object oldValue)
        {
            CustomVariable GetTeamIndex()
            {
                var variableDefinition = new CustomVariable();
                variableDefinition.Name = nameof(IDamageable.TeamIndex);
                variableDefinition.DefaultValue = 0;
                variableDefinition.Type = "int";
                variableDefinition.CreatesProperty = true;
                //variableDefinition.Category = "Damage Dealing";
                return variableDefinition;
            }

            CustomVariable GetSecondsBetweenDamage()
            {
                var secondsBetweenDamage = new CustomVariable();
                secondsBetweenDamage.Name = nameof(IDamageArea.SecondsBetweenDamage);
                secondsBetweenDamage.DefaultValue = 0.0;
                secondsBetweenDamage.Type = "double";
                secondsBetweenDamage.CreatesProperty = true;
                //secondsBetweenDamage.Category = "Damage Dealing";
                return secondsBetweenDamage;
            }

            var wereVariablesAddedOrRemoved = false;

            var currentEntity = GlueState.Self.CurrentEntitySave;

            if (currentEntity != null)
            {
                var teamIndexVariable = currentEntity.GetCustomVariable(nameof(IDamageable.TeamIndex));
                var secondsBetweenDamage = currentEntity.GetCustomVariable(nameof(IDamageArea.SecondsBetweenDamage));



                if (changedMember == "ImplementsIDamageArea")
                {
                    if(DamageDealingCodeGenerator.ImplementsIDamageArea(currentEntity))
                    {
                        if(teamIndexVariable == null)
                        {
                            currentEntity.CustomVariables.Add(GetTeamIndex());
                            wereVariablesAddedOrRemoved = true;
                        }
                        if (secondsBetweenDamage == null)
                        {
                            currentEntity.CustomVariables.Add(GetSecondsBetweenDamage());
                            wereVariablesAddedOrRemoved = true;
                        }
                    }
                    else
                    {
                        if(teamIndexVariable != null)
                        {
                            currentEntity.CustomVariables.Remove(teamIndexVariable);
                            wereVariablesAddedOrRemoved = true;
                        }
                        if (secondsBetweenDamage != null)
                        {
                            currentEntity.CustomVariables.Remove(secondsBetweenDamage);
                            wereVariablesAddedOrRemoved = true;
                        }
                    }
                }
                else if(changedMember == "ImplementsIDamageable")
                {
                    if (DamageDealingCodeGenerator.ImplementsIDamageable(currentEntity))
                    {
                        if (teamIndexVariable == null)
                        {
                            currentEntity.CustomVariables.Add(GetTeamIndex());
                            wereVariablesAddedOrRemoved = true;
                        }
                    }
                    else
                    {
                        if (teamIndexVariable != null)
                        {
                            currentEntity.CustomVariables.Remove(teamIndexVariable);
                            wereVariablesAddedOrRemoved = true;
                        }
                    }
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

        //private IEnumerable<VariableDefinition> HandleGetVariableDefinitionsForElement(IElement element)
        //{
        //    EntitySave entitySave = element as EntitySave;
        //    if(entitySave == null)
        //    {
        //        yield break;
        //    }

        //    if (entitySave != null)
        //    {
        //        VariableDefinition GetTeamIndex()
        //        {
        //            var variableDefinition = new VariableDefinition();
        //            variableDefinition.Name = nameof(IDamageable.TeamIndex);
        //            variableDefinition.DefaultValue = "0";
        //            variableDefinition.Type = "int";
        //            variableDefinition.Category = "Damage Dealing";
        //            return variableDefinition;
        //        }

        //        if (DamageDealingCodeGenerator.ImplementsIDamageable(entitySave))
        //        {
        //            yield return GetTeamIndex();
        //        }
        //        if (DamageDealingCodeGenerator.ImplementsIDamageArea(entitySave))
        //        {
        //            var secondsBetweenDamage = new VariableDefinition();
        //            secondsBetweenDamage.Name = nameof(IDamageArea.SecondsBetweenDamage);
        //            secondsBetweenDamage.DefaultValue = "0";
        //            secondsBetweenDamage.Type = "int";
        //            secondsBetweenDamage.Category = "Damage Dealing";

        //            yield return secondsBetweenDamage;

        //            yield return GetTeamIndex();
        //        }
        //    }
        //}

        internal static void HandleDisplayedEntity(EntitySave entitySave, EntitySavePropertyGridDisplayer displayer)
        {
            // Only show this if this is an ICollidable
            if (entitySave.IsICollidableRecursive())
            {
                var member = displayer.IncludeCustomPropertyMember("ImplementsIDamageArea", typeof(bool));

                member.SetCategory("Inheritance and Interfaces");
            }

            var damageableMember = displayer.IncludeCustomPropertyMember("ImplementsIDamageable", typeof(bool));
            damageableMember.SetCategory("Inheritance and Interfaces");
        }
    }
}
