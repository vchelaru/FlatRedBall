using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Instructions.Reflection;
using WpfDataUi.DataTypes;
using Gum.DataTypes.Variables;

namespace OfficialPlugins.VariableDisplay
{
    static partial class NamedObjectVariableShowingLogic
    {
        // todo - make this not static:
        private static void CreateCategoriesAndVariables(NamedObjectSave instance, GlueElement container,
            List<MemberCategory> categories, AssetTypeInfo ati)
        {
            // This defines the variable definitions, where the key is the name of the variable
            // on the instance, and the VariableDefinition is the root variable definition.
            // Note that the variable name will often match the VariableDefinition name, but not necessarily,
            // if the NamedObjectSave has tunneled the variable.
            Dictionary<string, VariableDefinition> variableDefinitions = GetVariableDefinitions(instance, ati, container);

            foreach (var kvp in variableDefinitions)
            {
                var variableDefinition = kvp.Value;
                var variableName = kvp.Key;

                // October 3, 2023
                // We used to use the
                // typedMember.CustomTypeName
                // here, but that was actually never
                // set to anything. We can just pass null
                // and save ourselves having to use typedMember
                //var instanceMember = CreateInstanceMember(instance, container, typedMember.CustomTypeName, ati, variableDefinition, variableName, categories);
                //TypedMemberBase typedMember = null;
                //typedMember = TypedMemberBase.GetTypedMember(variableName, type);
                var instanceMember = CreateInstanceMember(instance, container, null, ati, variableDefinition, variableName, categories);
                if (instanceMember != null)
                {
                    var categoryToAddTo = GetOrCreateCategoryToAddTo(categories, ati, variableName, variableDefinition);
                    categoryToAddTo.Members.Add(instanceMember);
                }

            }

            bool shouldAddSourceNameVariable = instance.SourceType == SourceType.File &&
                !string.IsNullOrEmpty(instance.SourceFile);

            if (shouldAddSourceNameVariable)
            {
                AddSourceNameVariable(instance, categories);

            }
        }

        private static void AddForTypedMember(NamedObjectSave instance, GlueElement container, List<MemberCategory> categories,
            AssetTypeInfo ati, TypedMemberBase typedMember, VariableDefinition variableDefinition)
        {
            variableDefinition = variableDefinition ?? ati?.VariableDefinitions.FirstOrDefault(item => item.Name == typedMember.MemberName);
            InstanceMember instanceMember = CreateInstanceMember(instance, container, typedMember.CustomTypeName, ati, variableDefinition, typedMember.MemberName, categories);

            var categoryToAddTo = GetOrCreateCategoryToAddTo(categories, ati, typedMember.CustomTypeName, variableDefinition);

            if (instanceMember != null)
            {
                categoryToAddTo.Members.Add(instanceMember);
            }
        }

        private static void AddSourceNameVariable(NamedObjectSave instance, List<MemberCategory> categories)
        {
            var categoryToAddTo = new MemberCategory(Localization.Texts.File);
            categoryToAddTo.FontSize = 14;

            if (categories.Count > 0)
            {
                categories.Insert(0, categoryToAddTo);
            }
            else
            {
                categories.Add(categoryToAddTo);
            }

            var instanceMember = CreateInstanceMemberForSourceName(instance);

            categoryToAddTo.Members.Add(instanceMember);
        }

        public static bool AssignVariableSubtext(NamedObjectSave instance, List<MemberCategory> categories, AssetTypeInfo assetTypeInfo)
        {
            var xVariable = categories.SelectMany(item => item.Members).FirstOrDefault(item => item.DisplayName == "X");
            var yVariable = categories.SelectMany(item => item.Members).FirstOrDefault(item => item.DisplayName == "Y");
            var zVariable = categories.SelectMany(item => item.Members).FirstOrDefault(item => item.DisplayName == "Z");

            string subtext = null;

            bool setZ = false;

            if (assetTypeInfo == AvailableAssetTypes.CommonAtis.Sprite)
            {
                // could this be plugin somehow?
                #region Check if the Sprite has animations:

                var animationChainsVariable = instance.GetCustomVariable("AnimationChains");
                var useAnimationPositionVariable = instance.GetCustomVariable("UseAnimationRelativePosition");
                var useAnimationPosition = useAnimationPositionVariable == null || (useAnimationPositionVariable.Value is bool asBool && asBool);

                if (!string.IsNullOrEmpty(animationChainsVariable?.Value as string) && useAnimationPosition)
                {
                    subtext = "This value may be overwritten by the Sprite's animation";
                }

                #endregion

            }

            if (assetTypeInfo?.IsPositionedObject == true && instance.IsContainer)
            {
                subtext = "This value may not be applied since this object has IsContainer set to true";
                setZ = true;
            }

            var changed = false;
            if (xVariable != null)
            {
                if (xVariable.DetailText != subtext)
                {
                    changed = true;
                    xVariable.DetailText = subtext;
                }
            }


            if (yVariable != null)
            {
                if (yVariable.DetailText != subtext)
                {
                    changed = true;
                    yVariable.DetailText = subtext;
                }
            }

            if (zVariable != null && setZ)
            {
                if(zVariable.DetailText != subtext)
                {
                    changed = true;
                    zVariable.DetailText = subtext;
                }
            }

            return changed;
        }

        private static MemberCategory CreateTopmostCategory(List<MemberCategory> categories)
        {
            MemberCategory topmostCategory = new MemberCategory();
            topmostCategory.Name = "";
            topmostCategory.HideHeader = true;
            categories.Insert(0, topmostCategory);
            return topmostCategory;
        }

        private static MemberCategory GetOrCreateCategoryToAddTo(List<MemberCategory> categories, AssetTypeInfo? ati,
            string memberName, VariableDefinition variableDefinition = null)
        {
            // By defaut make the last category get used (this is "Variables")
            var categoryToAddTo = categories.Last();
            // If there is an AssetTypeInfo...

            string? categoryName = null;

            if (ati != null || variableDefinition != null)
            {
                // ... see if there is avariable definition for this variable...
                var foundVariableDefinition = variableDefinition ?? ati!.VariableDefinitions.FirstOrDefault(item => item.Name == memberName);
                if (foundVariableDefinition != null)
                {
                    //... if so, see the category that it's a part of...
                    categoryName = foundVariableDefinition.Category;
                }
            }

            if (!string.IsNullOrEmpty(categoryName))
            {
                //... if a category is defined, see if we have a MemberCategory that we've created for it...
                categoryToAddTo = categories.FirstOrDefault(item => item.Name == categoryName);

                if (categoryToAddTo == null)
                {
                    //... if not, make one, and insert it before the last:
                    categoryToAddTo = new MemberCategory(categoryName);
                    categoryToAddTo.FontSize = 14;

                    categories.Insert(categories.Count - 1, categoryToAddTo);
                }
            }

            return categoryToAddTo;
        }

        private static void SortCategoriesAndMembers(ref List<MemberCategory> categories, AssetTypeInfo ati)
        {
            categories = SortCategories(categories, ati);

            SortMembers(categories, ati);
        }

        private static void SortMembers(List<MemberCategory> categories, AssetTypeInfo ati)
        {
            foreach (var category in categories)
            {
                string categoryName = category.Name;

                var variableDefinitions = ati.VariableDefinitions
                    .Where(item => item.Category == categoryName)
                    .Select(item => item.Name)
                    .ToList();

                var sorted = category.Members
                    .OrderBy(item =>
                    {
                        var castedItem = (DataGridItem)item;
                        var index = variableDefinitions.IndexOf(castedItem.UnmodifiedVariableName);

                        if (index == -1)
                        {
                            return int.MaxValue;
                        }
                        else
                        {
                            return index;
                        }
                    })
                    .ToList();

                category.Members.Clear();

                foreach (var item in sorted)
                {
                    category.Members.Add(item);
                }
            }
        }

        private static List<MemberCategory> SortCategories(List<MemberCategory> categories, AssetTypeInfo ati)
        {
            var orderedCategoryNames = ati.VariableDefinitions.Select(item => item.Category).Distinct().ToList();

            categories = categories.OrderBy(item =>
            {
                int index = orderedCategoryNames.IndexOf(item.Name);

                if (index == -1)
                {
                    return int.MaxValue;
                }
                else
                {
                    return index;
                }
            }).ToList();
            return categories;
        }
    }
}
