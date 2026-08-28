using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SetVariable;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Utilities;
using Glue;
using WpfDataUi;
using WpfDataUi.DataTypes;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using System;
using OfficialPlugins.VariableDisplay.Controls;
using OfficialPlugins.VariableDisplay.Data;
using GluePropertyGridClasses.StringConverters;
using FlatRedBall.Glue.Managers;
using WpfDataUi.Controls;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Errors;
using OfficialPlugins.PropertyGrid.Managers;
using System.ComponentModel;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;
using EditorObjects.IoC;

using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using OfficialPlugins.PropertyGrid;
using System.Windows.Controls;
using Gum.DataTypes.Variables;

namespace OfficialPlugins.VariableDisplay
{
    static partial class NamedObjectVariableShowingLogic
    {
        /// <summary>
        /// Builds the full category/member list for a single NamedObjectSave, without touching a grid.
        /// Used both for single-selection display (UpdateShownVariables) and to build one instance's
        /// contribution to a multi-selection display (UpdateShownVariablesForMultipleObjects).
        /// </summary>
        static List<MemberCategory> BuildCategories(NamedObjectSave instance, GlueElement container,
            AssetTypeInfo assetTypeInfo)
        {
            List<MemberCategory> categories = new List<MemberCategory>();
            var defaultCategory = new MemberCategory("Variables");
            defaultCategory.FontSize = 14;
            categories.Add(defaultCategory);

            assetTypeInfo = assetTypeInfo ?? instance.GetAssetTypeInfo();

            CreateCategoriesAndVariables(instance, container as GlueElement, categories, assetTypeInfo);

            if (assetTypeInfo != null)
            {
                SortCategoriesAndMembers(ref categories, assetTypeInfo);
            }

            if (assetTypeInfo != null)
            {
                AssignVariableSubtext(instance, categories, assetTypeInfo);
            }


            if (defaultCategory.Members.Count == 0)
            {
                categories.Remove(defaultCategory);
            }
            else if (categories.Count != 1)
            {
                defaultCategory.Name = "Other Variables";
            }

            if (categories.Count != 0)
            {
                MemberCategory topmostCategory = CreateTopmostCategory(categories);

                // "Name" should be the very first property:
                topmostCategory.Members.Add(CreateNameInstanceMember(instance));
                topmostCategory.Members.Add(CreateIsLockedMember(instance));
            }

            return categories;
        }

        public static void UpdateShownVariables(DataUiGrid grid, NamedObjectSave instance, GlueElement container,
            AssetTypeInfo assetTypeInfo = null)
        {
            assetTypeInfo = assetTypeInfo ?? instance.GetAssetTypeInfo();

            var categories = BuildCategories(instance, container, assetTypeInfo);

            var needsFullRefresh = GetIfNeedsFullRefresh(grid.Categories?.ToArray(), categories?.ToArray());
            if (needsFullRefresh)
            {
                grid.Categories.Clear();

                foreach (var category in categories)
                {
                    grid.Categories.Add(category);
                }

                grid.Refresh();
            }
            else
            {
                var ati = instance.GetAssetTypeInfo();
                Dictionary<string, VariableDefinition> variableDefinitions = GetVariableDefinitions(instance, ati, container);

                for (int i = 0; i < grid.Categories.Count; i++)
                {
                    var oldCategory = grid.Categories[i];

                    for (int j = 0; j < oldCategory.Members.Count; j++)
                    {
                        var oldMember = oldCategory.Members[j];

                        var newMember = categories[i].Members[j];

                        if (oldMember is NamedObjectSaveVariableDataGridItem memberAsNamedObjectSaveVariableDataGridItem)
                        {
                            var nameOnInstance = (newMember as NamedObjectSaveVariableDataGridItem).NameOnInstance;

                            // The variable definitions are re-derived from the instance's current
                            // AssetTypeInfo, which can lose entries that the grid's existing members
                            // were built from - for example if the instance's source file has gone
                            // missing since the grid was last populated. Skip refreshing this member
                            // rather than crashing; a full refresh will replace it once one is triggered.
                            if (variableDefinitions.TryGetValue(nameOnInstance, out var variableDefinition))
                            {
                                memberAsNamedObjectSaveVariableDataGridItem.RefreshFrom(instance, variableDefinition: variableDefinition, container: container, categories: grid.Categories, customTypeName: null,
                                    nameOnInstance: nameOnInstance);
                                memberAsNamedObjectSaveVariableDataGridItem.DetailText = newMember.DetailText;
                            }
                        }
                        else
                        {
                            // This isn't a NamedObjectSaveVariableDataGridItem instance, so we have to do a full replace since this type
                            // doesn't know how to refresh itself
                            oldCategory.Members[j] = categories[i].Members[j];
                        }
                    }
                }

                grid.Refresh();
            }
        }

        /// <summary>
        /// Populates the grid for a multi-object selection, using WpfDataUi's SetMultipleCategoryLists
        /// to wrap each shared property in a MultiSelectInstanceMember. Editing such a property is then
        /// wired (see WireUpBatchedMultiSet) to apply to every selected object in a single batched
        /// GluxCommands.SetVariableOnList call rather than one independent SetVariableOn per object.
        /// </summary>
        public static void UpdateShownVariablesForMultipleObjects(DataUiGrid grid, IReadOnlyList<NamedObjectSave> instances,
            GlueElement container, AssetTypeInfo assetTypeInfoOverride = null)
        {
            var listOfCategories = new List<List<MemberCategory>>();

            foreach (var instance in instances)
            {
                var categories = BuildCategories(instance, container, assetTypeInfoOverride ?? instance.GetAssetTypeInfo());
                RemoveMembersNotAllowedInMultiSelect(categories);
                listOfCategories.Add(categories);
            }

            grid.SetMultipleCategoryLists(listOfCategories);

            WireUpBatchedMultiSet(grid);
        }

        // Renaming is destructive if applied identically to every selected object (it would give them
        // all the same name), so unlike every other property, "Name" is not editable during multi-select:
        static void RemoveMembersNotAllowedInMultiSelect(List<MemberCategory> categories)
        {
            foreach (var category in categories)
            {
                var toRemove = category.Members
                    .Where(item => item is DataGridItem dataGridItem && dataGridItem.UnmodifiedVariableName == "Name")
                    .ToList();

                foreach (var member in toRemove)
                {
                    category.Members.Remove(member);
                }
            }
        }

        static void WireUpBatchedMultiSet(DataUiGrid grid)
        {
            foreach (var category in grid.Categories)
            {
                foreach (var member in category.Members)
                {
                    if (member is MultiSelectInstanceMember multiSelectMember &&
                        multiSelectMember.InstanceMembers.Count > 0 &&
                        multiSelectMember.InstanceMembers.All(item => item is NamedObjectSaveVariableDataGridItem))
                    {
                        List<NosVariableAssignment> batch = null;

                        multiSelectMember.BeforeMultiSet += args =>
                        {
                            batch = new List<NosVariableAssignment>();
                            foreach (NamedObjectSaveVariableDataGridItem inner in multiSelectMember.InstanceMembers)
                            {
                                inner.MultiSetBatchTarget = batch;
                            }
                        };

                        multiSelectMember.AfterMultiSet += args =>
                        {
                            foreach (NamedObjectSaveVariableDataGridItem inner in multiSelectMember.InstanceMembers)
                            {
                                inner.MultiSetBatchTarget = null;
                            }

                            if (batch?.Count > 0)
                            {
                                var isFullCommit = args.CommitType == SetPropertyCommitType.Full;
                                _ = GlueCommands.Self.GluxCommands.SetVariableOnList(batch,
                                    performSaveAndGenerateCode: isFullCommit,
                                    updateUi: true,
                                    recordUndo: isFullCommit);
                            }

                            batch = null;
                        };
                    }
                }
            }
        }

        public static void UpdateConditionalVisibility(DataUiGrid grid, NamedObjectSave instance, GlueElement container, AssetTypeInfo ati)
        {
            var needsFullRefresh = false;
            foreach (var category in grid.Categories)
            {
                foreach (var member in category.Members)
                {
                    if (member is NamedObjectSaveVariableDataGridItem namedObjectSaveVariableDataGridItem)
                    {
                        if (namedObjectSaveVariableDataGridItem.VariableDefinition.IsVariableVisibleInEditor != null)
                        {
                            // Is it true?
                            var shouldBeVisible = namedObjectSaveVariableDataGridItem.VariableDefinition.IsVariableVisibleInEditor(container, instance);
                            if (!shouldBeVisible)
                            {
                                // this shouldn't be here, we need a refresh
                                needsFullRefresh = true;
                            }
                        }
                    }
                }
            }

            if (!needsFullRefresh && ati != null)
            {
                foreach (var variableDefinition in ati.VariableDefinitions)
                {
                    if (variableDefinition.IsVariableVisibleInEditor != null)
                    {
                        var shouldBeShown = variableDefinition.IsVariableVisibleInEditor(container, instance);
                        if (shouldBeShown)
                        {
                            var wasFound = false;
                            // This better be in one of the categories
                            foreach (var category in grid.Categories)
                            {
                                foreach (var member in category.Members)
                                {
                                    if (member is NamedObjectSaveVariableDataGridItem namedObjectSaveVariableDataGridItem)
                                    {
                                        if (namedObjectSaveVariableDataGridItem.VariableDefinition.Name == variableDefinition.Name)
                                        {
                                            // This is good, we found it
                                            wasFound = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            if (!wasFound)
                            {
                                needsFullRefresh = true;
                                break;
                            }
                        }

                    }
                }
            }

            if (needsFullRefresh)
            {
                UpdateShownVariables(grid, instance, container, ati);
            }
        }

        static bool GetIfNeedsFullRefresh(MemberCategory[] oldCategories, MemberCategory[] newCategories)
        {
            if (oldCategories == null)
            {
                return true;
            }
            if (oldCategories.Length != newCategories.Length)
            {
                return true;
            }
            for (int i = 0; i < oldCategories.Length; i++)
            {
                var oldCategory = oldCategories[i];
                var newCategory = newCategories[i];
                if (oldCategory.Name != newCategory.Name ||
                    oldCategory.Members.Count != newCategory.Members.Count)
                {
                    return true;
                }

                for (int j = 0; j < oldCategory.Members.Count; j++)
                {
                    if (oldCategory.Members[j].Name != newCategory.Members[j].Name)
                    {
                        return true;
                    }
                    if (oldCategory.Members[j].PropertyType != newCategory.Members[j].PropertyType)
                    {
                        return true;
                    }
                }
            }

            // They match, does not need full refresh
            return false;
        }
    }
}
