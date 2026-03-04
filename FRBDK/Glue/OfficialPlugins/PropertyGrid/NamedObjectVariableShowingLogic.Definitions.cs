using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.PropertyGrid.Managers;
using Gum.DataTypes.Variables;

namespace OfficialPlugins.VariableDisplay
{
    static partial class NamedObjectVariableShowingLogic
    {
        private static Dictionary<string, VariableDefinition> GetVariableDefinitions(NamedObjectSave instance, AssetTypeInfo ati, GlueElement container)
        {
            Dictionary<string, VariableDefinition> variableDefinitions = new Dictionary<string, VariableDefinition>();

            if (ati?.VariableDefinitions.Count > 0)
            {

                foreach (var definition in ati.VariableDefinitions)
                {
                    var shouldAdd = true;

                    if (definition.IsVariableVisibleInEditor != null)
                    {
                        shouldAdd = definition.IsVariableVisibleInEditor(container, instance);
                    }

                    if (shouldAdd)
                    {
                        variableDefinitions[definition.Name] = definition;
                    }
                }
            }
            else if (instance.SourceType == SourceType.Entity)
            {
                var instanceElement = ObjectFinder.Self.GetElement(instance);
                if (instanceElement != null)
                {
                    for (int i = 0; i < instanceElement.CustomVariables.Count; i++)
                    {
                        var variable = instanceElement.CustomVariables[i];
                        VariableDefinition baseVariableDefinition = null;
                        if (instanceElement != null)
                        {
                            var variableInElement = instanceElement.GetCustomVariable(variable.Name);
                            var baseVariable = ObjectFinder.Self.GetBaseCustomVariable(variableInElement);
                            if (!string.IsNullOrEmpty(baseVariable?.SourceObject))
                            {
                                var ownerNos = instanceElement.GetNamedObjectRecursively(baseVariable.SourceObject);

                                var ownerNosAti = ownerNos.GetAssetTypeInfo();
                                baseVariableDefinition = ownerNosAti?.VariableDefinitions
                                    .FirstOrDefault(item => item.Name == baseVariable.SourceObjectProperty);
                            }
                            // This could be null if the ownerNos doesn't have an ATI.
                            if (variableInElement != null && baseVariableDefinition == null)
                            {
                                // we can create a new VariableDefinition here with the category:
                                baseVariableDefinition = new VariableDefinition();
                                //todo - may need to use culture invariant here...
                                //baseVariableDefinition.DefaultValue = variableInElement.DefaultValue?.To;
                                baseVariableDefinition.Name = variableInElement.Name;
                                baseVariableDefinition.Category = variableInElement.Category;
                                baseVariableDefinition.Type = variableInElement.Type;

                                if (variableInElement.VariableDefinition != null)
                                {
                                    baseVariableDefinition.MinValue = variableInElement.VariableDefinition.MinValue;
                                    baseVariableDefinition.MaxValue = variableInElement.VariableDefinition.MaxValue;
                                }

                                var subtext = variableInElement?.Summary ?? baseVariable?.Summary;
                                if (!string.IsNullOrWhiteSpace(subtext))
                                {
                                    baseVariableDefinition.SubtextFunc = (_, _) => subtext;
                                }

                                if (variableInElement.CustomGetForcedOptionsFunc != null)
                                {
                                    baseVariableDefinition.CustomGetForcedOptionFunc = (element, namedObject, referencedFileSave) => variableInElement.CustomGetForcedOptionsFunc(instanceElement);

                                }

                                if (!string.IsNullOrWhiteSpace(variableInElement.PreferredDisplayerTypeName) &&
                                    VariableDisplayerTypeManager.TypeNameToTypeAssociations.ContainsKey(variableInElement.PreferredDisplayerTypeName))
                                {
                                    baseVariableDefinition.PreferredDisplayer = VariableDisplayerTypeManager.TypeNameToTypeAssociations
                                        [variableInElement.PreferredDisplayerTypeName];
                                }
                                else if (variableInElement?.VariableDefinition?.PreferredDisplayer != null)
                                {
                                    baseVariableDefinition.PreferredDisplayer = variableInElement.VariableDefinition.PreferredDisplayer;

                                    if (variableInElement.VariableDefinition.PropertiesToSetOnDisplayer?.Count > 0)
                                    {
                                        baseVariableDefinition.PropertiesToSetOnDisplayer.Clear();

                                        foreach (var kvp in variableInElement.VariableDefinition.PropertiesToSetOnDisplayer)
                                        {
                                            baseVariableDefinition.PropertiesToSetOnDisplayer[kvp.Key] = kvp.Value;
                                        }
                                    }
                                }
                            }
                        }

                        if (baseVariableDefinition != null)
                        {
                            variableDefinitions.Add(variable.Name, baseVariableDefinition);
                        }
                    }

                }
            }

            return variableDefinitions;
        }
    }
}
