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
    static class NamedObjectVariableShowingLogic
    {
        #region Create InstanceMember (Variable)
        private static NamedObjectSaveVariableDataGridItem CreateInstanceMember(NamedObjectSave instance,
            GlueElement container,
            string customTypeName,
            AssetTypeInfo ati,
            VariableDefinition variableDefinition, string nameOnInstance, IEnumerable<MemberCategory> categories)
        {
            bool shouldBeSkipped = 
                GetIfShouldBeSkipped(variableDefinition.Name, instance, ati);
            ///////Early Out//////////
            if (shouldBeSkipped)
            {
                return null;
            }
            ////End Early Out///////

            var instanceMember = new NamedObjectSaveVariableDataGridItem();
            instanceMember.RefreshFrom(instance, variableDefinition, container, categories, customTypeName, nameOnInstance);
            instanceMember.RefreshAddContextMenuEvents();

            return instanceMember;
        }

        #endregion

        #region Set Variable Value



        #endregion


        private static void CreateCategoriesAndVariables(NamedObjectSave instance, GlueElement container,
            List<MemberCategory> categories, AssetTypeInfo ati)
        {
            // This defines the variable definitions, where the key is the name of the variable
            // on the instance, and the VariableDefinition is the root variable definition.
            // Note that the variable name will often match the VariableDefinition name, but not necessarily,
            // if the NamedObjectSave has tunneled the variable.
            Dictionary<string, VariableDefinition> variableDefinitions = GetVariableDefinitions(instance, ati);

            foreach (var kvp in variableDefinitions)
            {
                var variableDefinition = kvp.Value;
                var variableName = kvp.Key;
                bool fallBackToTypedMember = false;
                try
                {
                    Type type = null;
                    if (!string.IsNullOrWhiteSpace(variableDefinition.Type))
                    {
                        type = FlatRedBall.Glue.Parsing.TypeManager.GetTypeFromString(variableDefinition.Type);
                    }

                    if (type == null)
                    {
                        fallBackToTypedMember = true;
                    }
                    else
                    {
                        TypedMemberBase typedMember = null;
                        typedMember = TypedMemberBase.GetTypedMember(variableName, type);
                        var instanceMember = CreateInstanceMember(instance, container, typedMember.CustomTypeName, ati, variableDefinition, variableName, categories);
                        if (instanceMember != null)
                        {
                            var categoryToAddTo = GetOrCreateCategoryToAddTo(categories, ati, typedMember, variableDefinition);
                            categoryToAddTo.Members.Add(instanceMember);
                        }
                    }
                }
                catch
                {
                    fallBackToTypedMember = true;
                }

                if (fallBackToTypedMember)
                {
                    // this new code isn't working with some things like generics. Until I fix that, let's fall back:

                    var typedMember = instance.TypedMembers.FirstOrDefault(item => item.MemberName == variableName);

                    if (typedMember != null)
                    {
                        AddForTypedMember(instance, container, categories, ati, typedMember, variableDefinition);
                    }
                }
            }

            bool shouldAddSourceNameVariable = instance.SourceType == SourceType.File &&
                !string.IsNullOrEmpty(instance.SourceFile);

            if (shouldAddSourceNameVariable)
            {
                AddSourceNameVariable(instance, categories);

            }
        }

        private static Dictionary<string, VariableDefinition> GetVariableDefinitions(NamedObjectSave instance, AssetTypeInfo ati)
        {
            Dictionary<string, VariableDefinition> variableDefinitions = new Dictionary<string, VariableDefinition>();

            if (ati?.VariableDefinitions.Count > 0)
            {
                foreach (var definition in ati.VariableDefinitions)
                {
                    variableDefinitions[definition.Name] = definition;

                }
            }
            else
            {
                var instanceElement = ObjectFinder.Self.GetElement(instance);
                for (int i = 0; i < instance.TypedMembers.Count; i++)
                {
                    VariableDefinition baseVariableDefinition = null;
                    TypedMemberBase typedMember = instance.TypedMembers[i];
                    if (instanceElement != null)
                    {
                        var variableInElement = instanceElement.GetCustomVariable(typedMember.MemberName);
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
                        }
                    }

                    if (baseVariableDefinition != null)
                    {
                        variableDefinitions.Add(typedMember.MemberName, baseVariableDefinition);
                    }
                }
            }

            return variableDefinitions;
        }

        public static void UpdateShownVariables(DataUiGrid grid, NamedObjectSave instance, GlueElement container,
            AssetTypeInfo assetTypeInfo = null)
        {
            #region Initial logic


            List<MemberCategory> categories = new List<MemberCategory>();
            var defaultCategory = new MemberCategory("Variables");
            defaultCategory.FontSize = 14;
            categories.Add(defaultCategory);

            assetTypeInfo = assetTypeInfo ?? instance.GetAssetTypeInfo();

            // not sure if this is needed:
            if (instance.TypedMembers.Count == 0)
            {
                instance.UpdateCustomProperties();
            }

            #endregion

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

            var needsFullRefresh = GetIfNeedsFullRefresh(grid.Categories?.ToArray(), categories?.ToArray());
            if(needsFullRefresh )
            {
                grid.Categories.Clear();
                SetAlternatingColors(grid, categories);

                foreach (var category in categories)
                {
                    grid.Categories.Add(category);
                }

                grid.Refresh();
            }
            else
            {
                var ati = instance.GetAssetTypeInfo();
                Dictionary<string, VariableDefinition> variableDefinitions = GetVariableDefinitions(instance, ati);

                for(int i = 0; i < grid.Categories.Count; i++)
                {
                    var oldCategory = grid.Categories[i];

                    for(int j = 0; j < oldCategory.Members.Count; j++)
                    {
                        var oldMember = oldCategory.Members[j];

                        var newMember = categories[i].Members[j];

                        if(oldMember is NamedObjectSaveVariableDataGridItem memberAsNamedObjectSaveVariableDataGridItem)
                        {
                            var nameOnInstance = (newMember as NamedObjectSaveVariableDataGridItem).NameOnInstance;

                            var variableDefinition = variableDefinitions[nameOnInstance];
                            Type type = null;
                            if (!string.IsNullOrWhiteSpace(variableDefinition.Type))
                            {
                                type = FlatRedBall.Glue.Parsing.TypeManager.GetTypeFromString(variableDefinition.Type);
                            }

                            TypedMemberBase typedMember = null;
                            if (type != null)
                            {
                                typedMember = TypedMemberBase.GetTypedMember(variableDefinition.Name, type);
                            }
                            memberAsNamedObjectSaveVariableDataGridItem.RefreshFrom(instance, variableDefinition:variableDefinition, container: container, categories: grid.Categories, customTypeName: typedMember?.CustomTypeName, 
                                nameOnInstance: nameOnInstance);
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

        static bool GetIfNeedsFullRefresh(MemberCategory[] oldCategories, MemberCategory[] newCategories)
        {
            if(oldCategories == null)
            {
                return true;
            }
            if(oldCategories.Length != newCategories.Length)
            {
                return true;
            }
            for(int i = 0; i < oldCategories.Length; i++)
            {
                var oldCategory = oldCategories[i];
                var newCategory = newCategories[i];
                if (oldCategory.Name != newCategory.Name ||
                    oldCategory.Members.Count != newCategory.Members.Count)
                {
                    return true;
                }

                for(int j = 0; j < oldCategory.Members.Count; j++)
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

        private static MemberCategory CreateTopmostCategory(List<MemberCategory> categories)
        {
            MemberCategory topmostCategory = new MemberCategory();
            topmostCategory.Name = "";
            topmostCategory.HideHeader = true;
            categories.Insert(0, topmostCategory);
            return topmostCategory;
        }

        public static void AssignVariableSubtext(NamedObjectSave instance, List<MemberCategory> categories, AssetTypeInfo assetTypeInfo)
        {
            var xVariable = categories.SelectMany(item => item.Members).FirstOrDefault(item => item.DisplayName == "X");
            var yVariable = categories.SelectMany(item => item.Members).FirstOrDefault(item => item.DisplayName == "Y");
            string subtext = string.Empty;
            if (assetTypeInfo == AvailableAssetTypes.CommonAtis.Sprite)
            {
                // could this be plugin somehow?
                var animationChainsVariable = instance.GetCustomVariable("AnimationChains");
                var useAnimationPositionVariable = instance.GetCustomVariable("UseAnimationRelativePosition");
                var useAnimationPosition = useAnimationPositionVariable == null || (useAnimationPositionVariable.Value is bool asBool && asBool);

                if (!string.IsNullOrEmpty(animationChainsVariable?.Value as string) && useAnimationPosition)
                {
                    subtext = "This value may be overwritten by the Sprite's animation";
                }
            }

            if (xVariable != null)
            { xVariable.DetailText = subtext; }


            if (yVariable != null)
            { yVariable.DetailText = subtext; }
        }

        private static void SetAlternatingColors(DataUiGrid grid, List<MemberCategory> categories)
        {
            // skip the first category in putting the alternating colors:
            for (int i = 0; i < categories.Count; i++)
            {
                var category = categories[i];
                if (i != 0)
                {
                    const byte brightness = 227;
                    category.SetAlternatingColors(new SolidColorBrush(Color.FromRgb(brightness, brightness, brightness)), Brushes.Transparent);
                }
            }
        }

        private static void AddForTypedMember(NamedObjectSave instance, GlueElement container, List<MemberCategory> categories,
            AssetTypeInfo ati, TypedMemberBase typedMember, VariableDefinition variableDefinition)
        {
            variableDefinition = variableDefinition ?? ati?.VariableDefinitions.FirstOrDefault(item => item.Name == typedMember.MemberName);
            InstanceMember instanceMember = CreateInstanceMember(instance, container, typedMember.CustomTypeName, ati, variableDefinition, typedMember.MemberName, categories);

            var categoryToAddTo = GetOrCreateCategoryToAddTo(categories, ati, typedMember, variableDefinition);

            if (instanceMember != null)
            {
                categoryToAddTo.Members.Add(instanceMember);
            }
        }

        private static void AddSourceNameVariable(NamedObjectSave instance, List<MemberCategory> categories)
        {
            var categoryToAddTo = new MemberCategory("File");
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

        private static InstanceMember CreateInstanceMemberForSourceName(NamedObjectSave instance)
        {

            var instanceMember = new FileInstanceMember();

            instanceMember.View += () =>
            {
                var element = GlueState.Self.CurrentElement;
                var rfs = element.ReferencedFiles.FirstOrDefault(item => item.Name == instance.SourceFile);

                if (rfs != null)
                {
                    GlueCommands.Self.SelectCommands.Select(
                        rfs,
                        instance.SourceNameWithoutParenthesis);
                }
            };

            instanceMember.FirstGridLength = new System.Windows.GridLength(140);

            instanceMember.UnmodifiedVariableName = "SourceName";
            string fileName = FlatRedBall.IO.FileManager.RemovePath(instance.SourceFile);
            instanceMember.DisplayName = $"Object in {fileName}:";

            // todo: get the type converter from the file
            var typeConverter = new AvailableNameablesStringConverter(instance, null);
            instanceMember.TypeConverter = typeConverter;

            instanceMember.CustomGetTypeEvent += (throwaway) => typeof(string);

            instanceMember.PreferredDisplayer = typeof(FileReferenceComboBox);

            instanceMember.IsDefault = instance.SourceName == null;

            instanceMember.CustomGetEvent += (throwaway) =>
            {
                return instance.SourceName;
            };

            instanceMember.CustomSetEvent += (owner, value) =>
            {
                instanceMember.IsDefault = false;
                RefreshLogic.IgnoreNextRefresh();

                instance.SourceName = value as string;

                GlueCommands.Self.GluxCommands.SaveGlux();

                GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();

                GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
            };

            instanceMember.IsDefaultSet += (owner, args) =>
            {
                instance.SourceName = null;
            };

            instanceMember.SetValueError += (newValue) =>
            {
                if (newValue is string && string.IsNullOrEmpty(newValue as string))
                {
                    MakeDefault(instance, "SourceName");
                }
            };

            return instanceMember;

        }

        private static DataGridItem CreateNameInstanceMember(NamedObjectSave instance)
        {
            var instanceMember = new DataGridItem();
            instanceMember.DisplayName = "Name";
            instanceMember.UnmodifiedVariableName = "Name";

            // this gets updated in the CustomSetEvent below
            string oldValue = instance.InstanceName;

            if (instance.DefinedByBase)
            {
                instanceMember.MakeReadOnly();
            }

            instanceMember.CustomSetEvent += (throwaway, value) =>
            {
                instanceMember.IsDefault = false;
                RefreshLogic.IgnoreNextRefresh();

                instance.InstanceName = value as string;

                EditorObjects.IoC.Container.Get<SetPropertyManager>().ReactToPropertyChanged(
                    "InstanceName", oldValue, "InstanceName", null);


                //GlueCommands.Self.GluxCommands.SetVariableOn(
                //    instance,
                //    "Name",
                //    typeof(string),
                //    value);


                GlueCommands.Self.GluxCommands.SaveGlux();

                GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();

                GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();

                oldValue = (string)value;
            };
            instanceMember.CustomGetEvent += throwaway => instance.InstanceName;

            instanceMember.CustomGetTypeEvent += throwaway => typeof(string);

            return instanceMember;
        }

        private static DataGridItem CreateIsLockedMember(NamedObjectSave instance)
        {
            var instanceMember = new DataGridItem();
            instanceMember.DisplayName =
                StringFunctions.InsertSpacesInCamelCaseString(nameof(instance.IsEditingLocked));
            instanceMember.UnmodifiedVariableName =
                nameof(instance.IsEditingLocked);

            var oldValue = instance.IsEditingLocked;

            instanceMember.CustomSetEvent += (throwaway, value) =>
            {
                instanceMember.IsDefault = false;
                RefreshLogic.IgnoreNextRefresh();

                var valueAsBool = value as bool? ?? false;
                instance.IsEditingLocked = valueAsBool;

                EditorObjects.IoC.Container.Get<SetPropertyManager>().ReactToPropertyChanged(
                    nameof(instance.IsEditingLocked), oldValue, nameof(instance.IsEditingLocked), null);


                //GlueCommands.Self.GluxCommands.SetVariableOn(
                //    instance,
                //    "Name",
                //    typeof(string),
                //    value);


                GlueCommands.Self.GluxCommands.SaveGlux();

                GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();

                GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();

                oldValue = valueAsBool;
            };

            instanceMember.CustomGetEvent += throwaway =>
            {
                //return instance.IsEditingLocked;
                return ObjectFinder.Self.GetPropertyValueRecursively<bool>(instance, nameof(NamedObjectSave.IsEditingLocked));
            };

            instanceMember.IsDefaultSet += (sender, args) =>
            {
                instance.Properties.RemoveAll(item => item.Name == nameof(NamedObjectSave.IsEditingLocked));
            };

            instanceMember.CustomGetTypeEvent += throwaway => typeof(bool);

            return instanceMember;
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
                        var castedItem = item as DataGridItem;
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

        private static MemberCategory GetOrCreateCategoryToAddTo(List<MemberCategory> categories, AssetTypeInfo ati,
            TypedMemberBase typedMember, VariableDefinition variableDefinition = null)
        {
            // By defaut make the last category get used (this is "Variables")
            var categoryToAddTo = categories.Last();
            // If there is an AssetTypeInfo...

            string categoryName = null;

            if (ati != null || variableDefinition != null)
            {
                // ... see if there is avariable definition for this variable...
                var foundVariableDefinition = variableDefinition ?? ati.VariableDefinitions.FirstOrDefault(item => item.Name == typedMember.MemberName);
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




        /// <summary>
        /// Determines if a variable should be ignored (not displayed) by the variable plugin.
        /// </summary>
        /// <param name="typedMember">The typed member - represents the variable which may be ignored.</param>
        /// <param name="instance">The NamedObjectSave owning the variable.</param>
        /// <param name="ati">The Asset Typ Info for the NamedObjectSave.</param>
        /// <returns>Whether to skip the variable.</returns>
        private static bool GetIfShouldBeSkipped(string name, NamedObjectSave instance, AssetTypeInfo ati)
        {
            ///////////////////Early Out////////////////////////
            if (string.IsNullOrEmpty(name))
            {
                return true;
            }

            //////////////////End Early Out//////////////////////

            if (ati != null)
            {
                if (ati.IsPositionedObject)
                {
                    if (name.EndsWith("Velocity") || name.EndsWith("Acceleration") || name.StartsWith("Relative") ||
                        name == "ParentBone" || name == "KeepTrackOfReal" || name == "Drag"

                        )
                    {
                        return true;
                    }

                }

                if (ati.QualifiedRuntimeTypeName.QualifiedType == "FlatRedBall.Math.Geometry.AxisAlignedRectangle")
                {
                    return name == "ScaleX" || name == "ScaleY" || name == "Top" || name == "Bottom" ||
                        name == "Left" || name == "Right";
                }

                if (ati.QualifiedRuntimeTypeName.QualifiedType == "FlatRedBall.Graphics.Text")
                {
                    return
                        name == "AlphaRate" || name == "RedRate" || name == "GreenRate" || name == "BlueRate" ||
                        name == "ScaleVelocity" || name == "SpacingVelocity" ||
                        name == "ScaleXVelocity" || name == "ScaleYVelocity" ||
                        // These used to be the standard way to size text, but now we just
                        // use "TextureScale"
                        name == "Scale" || name == "Spacing" || name == "NewLineDistance"

                        ;
                }

                if (ati.QualifiedRuntimeTypeName.QualifiedType == "FlatRedBall.Camera")
                {
                    return
                        name == "AspectRatio" || name == "DestinationRectangle" || name == "CameraModelCullMode";

                }

                if (ati.QualifiedRuntimeTypeName.QualifiedType == "FlatRedBall.Math.Geometry.Polygon")
                {
                    return
                        name == "RotationX" || name == "RotationY" || name == "Points";
                }


                if (ati.QualifiedRuntimeTypeName.QualifiedType == "FlatRedBall.Graphics.Layer")
                {
                    return
                        name == "LayerCameraSettings";
                }

                if (ati.QualifiedRuntimeTypeName.QualifiedType == "FlatRedBall.Sprite")
                {
                    return
                        name == "AlphaRate" || name == "RedRate" || name == "GreenRate" || name == "BlueRate" ||
                        name == "RelativeTop" || name == "RelativeBottom" ||
                        name == "RelativeLeft" || name == "RelativeRight" ||
                        name == "TimeCreated" || name == "TimeIntoAnimation" ||
                        name == "ScaleX" || name == "ScaleY" ||
                        name == "CurrentChainIndex" ||
                        name == "Top" || name == "Bottom" || name == "Left" || name == "Right" ||
                        name == "PixelSize" ||
                        name == "LeftTextureCoordinate" || name == "RightTextureCoordinate" ||
                        name == "BottomTextureCoordinate" || name == "TopTextureCoordinate" ||
                        name == "ScaleXVelocity" || name == "ScaleYVelocity" ||
                        name == "TextureFilter"
                        ;

                }
            }


            EntitySave nosEntity = instance.SourceType == SourceType.Entity
                ? ObjectFinder.Self.GetEntitySave(instance.SourceClassType)
                : null;

            var variableInNos = nosEntity?.GetCustomVariableRecursively(name);
            CustomVariable baseNos = variableInNos != null
                ? ObjectFinder.Self.GetBaseCustomVariable(variableInNos)
                : null;

            var isSharedStatic = baseNos?.IsShared == true;

            /////////////////Early Out///////////////////////////
            if (isSharedStatic)
            {
                return true;
            }


            return false;
        }

        private static void MakeDefault(NamedObjectSave instance, string memberName)
        {
            var oldValue = instance.GetCustomVariable(memberName)?.Value;

            PropertyGridRightClickHelper.SetVariableToDefault(instance, memberName);

            var element = ObjectFinder.Self.GetElementContaining(instance);

            if (element != null)
            {
                // do we want to run this async?
                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
            }

            GlueCommands.Self.GluxCommands.SaveGlux();

            MainGlueWindow.Self.PropertyGrid.Refresh();

            PluginManager.ReactToChangedProperty(memberName, oldValue, element, new PluginManager.NamedObjectSavePropertyChange
            { 
                NamedObjectSave = instance,
                ChangedPropertyName = memberName
            });

            PluginManager.ReactToNamedObjectChangedValueList(new List<VariableChangeArguments>
            {
                new VariableChangeArguments
                {
                    NamedObject = instance,
                    ChangedMember = memberName,
                    OldValue = oldValue
                }
            });
        }


    }
}
