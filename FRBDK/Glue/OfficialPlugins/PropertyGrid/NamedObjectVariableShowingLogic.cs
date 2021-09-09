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

namespace OfficialPlugins.VariableDisplay
{
    static class NamedObjectVariableShowingLogic
    {
        public static void UpdateShownVariables(DataUiGrid grid, NamedObjectSave instance, IElement container,
            AssetTypeInfo assetTypeInfo = null)
        {
            grid.Categories.Clear();

            List<MemberCategory> categories = new List<MemberCategory>();
            var defaultCategory = new MemberCategory("Variables");
            defaultCategory.FontSize = 14;
            categories.Add(defaultCategory);

            if(assetTypeInfo == null)
            {
                assetTypeInfo = instance.GetAssetTypeInfo();
            }

            // not sure if this is needed:
            if (instance.TypedMembers.Count == 0)
            {
                instance.UpdateCustomProperties();
            }

            CreateCategoriesAndVariables(instance, container, categories, assetTypeInfo);



            if (assetTypeInfo != null)
            {
                SortCategoriesAndMembers(ref categories, assetTypeInfo);
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
                // "Name" should be the very first property:
                var nameCategory = CreateNameInstanceMember(instance);
                categories.Insert(0, nameCategory);
            }

            SetAlternatingColors(grid, categories);

            foreach(var category in categories)
            {
                grid.Categories.Add(category);
            }

            grid.Refresh();
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

        private static void CreateCategoriesAndVariables(NamedObjectSave instance, IElement container, 
            List<MemberCategory> categories, AssetTypeInfo ati)
        {
            // May 13, 2017
            // I'd like to get
            // completely rid of 
            // TypedMembers and move
            // to using the custom variables.
            // We'll try this out:
            if (ati?.VariableDefinitions != null && ati.VariableDefinitions.Count > 0)
            {
                foreach(var variableDefinition in ati.VariableDefinitions)
                {
                    bool fallBackToTypedMember = false;
                    try
                    {
                        var type = FlatRedBall.Glue.Parsing.TypeManager.GetTypeFromString(variableDefinition.Type);
                        TypedMemberBase typedMember = null;

                        if(type == null)
                        {
                            fallBackToTypedMember = true;
                        }
                        else
                        {
                            typedMember = TypedMemberBase.GetTypedMember(variableDefinition.Name, type);

                            InstanceMember instanceMember = CreateInstanceMember(instance, container, typedMember, ati, variableDefinition);


                            if (instanceMember != null)
                            {
                                var categoryToAddTo = GetOrCreateCategoryToAddTo(categories, ati, typedMember);
                                categoryToAddTo.Members.Add(instanceMember);
                            }
                        }
                    }
                    catch
                    {
                        fallBackToTypedMember = true;
                    }

                    if(fallBackToTypedMember)
                    {
                        // this new code isn't working with some things like generics. Until I fix that, let's fall back:

                        var typedMember = instance.TypedMembers.FirstOrDefault(item => item.MemberName == variableDefinition.Name);

                        if (typedMember != null)
                        {
                            AddForTypedMember(instance, container, categories, ati, typedMember);
                        }
                    }
                }
            }

            else // This is used when viewing a  NOS that is of entity type (no ATI)
            {
                var instanceElement = ObjectFinder.Self.GetElement(instance);
                for (int i = 0; i < instance.TypedMembers.Count; i++)
                {
                    VariableDefinition baseVariableDefinition = null;
                    TypedMemberBase typedMember = instance.TypedMembers[i];
                    if(instanceElement != null)
                    {
                        var variableInElement = instanceElement.GetCustomVariable(typedMember.MemberName);

                        if(variableInElement != null && !string.IsNullOrEmpty(variableInElement.SourceObject))
                        {
                            var ownerNos = instanceElement.GetNamedObjectRecursively(variableInElement.SourceObject);

                            var ownerNosAti = ownerNos.GetAssetTypeInfo();
                            baseVariableDefinition = ownerNosAti?.VariableDefinitions
                                .FirstOrDefault(item => item.Name == variableInElement.SourceObjectProperty);
                        }
                    }
                    AddForTypedMember(instance, container, categories, ati, typedMember, baseVariableDefinition);
                }
            }
            bool shouldAddSourceNameVariable = instance.SourceType == SourceType.File &&
                !string.IsNullOrEmpty(instance.SourceFile);

            if(shouldAddSourceNameVariable)
            {
                AddSourceNameVariable(instance, categories);

            }
        }

        private static void AddForTypedMember(NamedObjectSave instance, IElement container, List<MemberCategory> categories,
            AssetTypeInfo ati, TypedMemberBase typedMember, VariableDefinition variableDefinition = null)
        {
            variableDefinition = variableDefinition ?? ati?.VariableDefinitions.FirstOrDefault(item => item.Name == typedMember.MemberName);
            InstanceMember instanceMember = CreateInstanceMember(instance, container, typedMember, ati, variableDefinition);

            var categoryToAddTo = GetOrCreateCategoryToAddTo(categories, ati, typedMember);

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

        private static MemberCategory CreateNameInstanceMember(NamedObjectSave instance)
        {
            var instanceMember = new DataGridItem();
            instanceMember.DisplayName = "Name";
            instanceMember.UnmodifiedVariableName = "Name";

            // this gets updated in the CustomSetEvent below
            string oldValue = instance.InstanceName;

            if(instance.DefinedByBase)
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
            instanceMember.CustomGetEvent += (throwaway) =>
                {
                    return instance.InstanceName;
                };

            instanceMember.CustomGetTypeEvent += (throwaway) => typeof(string);

            MemberCategory category = new MemberCategory();
            category.Name = "";
            category.HideHeader = true;
            category.Members.Add(instanceMember);

            return category;
        }

        private static void SortCategoriesAndMembers(ref List<MemberCategory> categories, AssetTypeInfo ati)
        {
            categories = SortCategories(categories, ati);

            SortMembers(categories, ati);
        }

        private static void SortMembers(List<MemberCategory> categories, AssetTypeInfo ati)
        {
            foreach(var category in categories)
            {
                string categoryName = category.Name;

                var variableDefinitions = ati.VariableDefinitions
                    .Where(item => item.Category == categoryName)
                    .Select(item=>item.Name)
                    .ToList();

                var sorted = category.Members
                    .OrderBy(item =>
                    {
                        var castedItem = item as DataGridItem;
                        var index = variableDefinitions.IndexOf(castedItem.UnmodifiedVariableName);

                        if(index == -1)
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

                foreach(var item in sorted)
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

        private static MemberCategory GetOrCreateCategoryToAddTo(List<MemberCategory> categories, AssetTypeInfo ati, TypedMemberBase typedMember)
        {
            // By defaut make the last category get used (this is "Variables")
            var categoryToAddTo = categories.Last();
            // If there is an AssetTypeInfo...
            if (ati != null)
            {
                // ... see if there is avariable definition for this variable...
                var foundVariableDefinition = ati.VariableDefinitions.FirstOrDefault(item => item.Name == typedMember.MemberName);
                if (foundVariableDefinition != null)
                {
                    //... if so, see the category that it's a part of...
                    string categoryName = foundVariableDefinition.Category;

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
                }
            }
            return categoryToAddTo;
        }

        private static InstanceMember CreateInstanceMember(NamedObjectSave instance, IElement container, 
            TypedMemberBase typedMember, AssetTypeInfo ati, VariableDefinition variableDefinition)
        {
            bool shouldBeSkipped = GetIfShouldBeSkipped(typedMember, instance, ati);

            DataGridItem instanceMember = null;

            if (!shouldBeSkipped)
            {
                var typeConverter = PluginManager.GetTypeConverter(
                     container, instance, typedMember);

                bool isObjectInFile = typeConverter is IObjectsInFileConverter;

                var memberType = typedMember.MemberType;

                if(isObjectInFile)
                {
                    var fileInstanceMember = new FileInstanceMember();
                    instanceMember = fileInstanceMember;


                    fileInstanceMember.View += () =>
                    {
                        var rfs = (typeConverter as IObjectsInFileConverter).ReferencedFileSave;

                        if (rfs != null)
                        {
                            var value = fileInstanceMember.Value as string;

                            GlueCommands.Self.SelectCommands.Select(
                                rfs,
                                value);
                        }
                    };

                    instanceMember.PreferredDisplayer = typeof(FileReferenceComboBox);

                }
                else
                {
                    instanceMember = new DataGridItem();

                }

                if(variableDefinition?.PreferredDisplayer != null)
                {
                    instanceMember.PreferredDisplayer = variableDefinition.PreferredDisplayer;
                }
                else if(variableDefinition?.Name == "RotationZ" && variableDefinition.Type == "float")
                {
                    instanceMember.PreferredDisplayer = typeof(AngleSelectorDisplay);
                    instanceMember.PropertiesToSetOnDisplayer[nameof(AngleSelectorDisplay.TypeToPushToInstance)] =
                        AngleType.Radians;
                }
                else if(variableDefinition?.MinValue != null && variableDefinition?.MaxValue != null)
                {
                    instanceMember.PreferredDisplayer = typeof(SliderDisplay);
                    instanceMember.PropertiesToSetOnDisplayer[nameof(SliderDisplay.MaxValue)] =
                        variableDefinition.MaxValue.Value;
                    instanceMember.PropertiesToSetOnDisplayer[nameof(SliderDisplay.MinValue)] =
                        variableDefinition.MinValue.Value;
                }

                instanceMember.FirstGridLength = new System.Windows.GridLength(140);

                instanceMember.UnmodifiedVariableName = typedMember.MemberName;
                string displayName = StringFunctions.InsertSpacesInCamelCaseString(typedMember.MemberName);
                instanceMember.DisplayName = displayName;

                instanceMember.TypeConverter = typeConverter;

                // hack! Certain ColorOperations aren't supported in MonoGame. One day they will be if we ever get the
                // shader situation solved. But until then, these cause crashes so let's remove them.
                // Do this after setting the type converter
                if(variableDefinition?.Type == nameof(FlatRedBall.Graphics.ColorOperation))
                {
                    instanceMember.TypeConverter = null;
                    // one day?
                    instanceMember.CustomOptions.Add(FlatRedBall.Graphics.ColorOperation.Texture);
                    instanceMember.CustomOptions.Add(FlatRedBall.Graphics.ColorOperation.Add);
                    instanceMember.CustomOptions.Add(FlatRedBall.Graphics.ColorOperation.Color);
                    instanceMember.CustomOptions.Add(FlatRedBall.Graphics.ColorOperation.ColorTextureAlpha);
                    instanceMember.CustomOptions.Add(FlatRedBall.Graphics.ColorOperation.Modulate);
                    //instanceMember.CustomOptions.Add(FlatRedBall.Graphics.ColorOperation.Subtract);
                    //instanceMember.CustomOptions.Add(FlatRedBall.Graphics.ColorOperation.InverseTexture);
                    //instanceMember.CustomOptions.Add(FlatRedBall.Graphics.ColorOperation.Modulate2X);
                    //instanceMember.CustomOptions.Add(FlatRedBall.Graphics.ColorOperation.Modulate4X);
                    //instanceMember.CustomOptions.Add(FlatRedBall.Graphics.ColorOperation.InterpolateColor);
                }

                // Important - set the forced options after setting the type converter so they have "final say"
                if(variableDefinition?.ForcedOptions?.Count > 0)
                {
                    instanceMember.PreferredDisplayer = typeof(ComboBoxDisplay);
                    var list = new List<object>();
                    list.AddRange(variableDefinition.ForcedOptions);
                    instanceMember.CustomOptions = list;
                }
                else if(variableDefinition?.CustomGetForcedOptionFunc != null)
                {
                    instanceMember.PreferredDisplayer = typeof(ComboBoxDisplay);
                    var list = new List<object>();
                    list.AddRange(variableDefinition.CustomGetForcedOptionFunc(container, instance, null));
                    instanceMember.CustomOptions = list;
                }

                instanceMember.CustomGetTypeEvent += (throwaway) => memberType;


                instanceMember.IsDefault = instance.GetCustomVariable(typedMember.MemberName) == null;



                instanceMember.CustomGetEvent += (throwaway) =>
                {

                    var instruction = instance.GetCustomVariable(typedMember.MemberName);

                    if (instruction == null)
                    {
                        if (variableDefinition != null)
                        {
                            var toReturn = variableDefinition.DefaultValue;
                            if (memberType == typeof(bool))
                            {
                                bool boolToReturn = false;

                                bool.TryParse(variableDefinition.DefaultValue, out boolToReturn);

                                return boolToReturn;
                            }
                            else if(memberType == typeof(float))
                            {
                                float floatToReturn = 0.0f;

                                float.TryParse(variableDefinition.DefaultValue, out floatToReturn);

                                return floatToReturn;
                            }
                            else if(memberType == typeof(int))
                            {
                                int intToReturn = 0;

                                int.TryParse(variableDefinition.DefaultValue, out intToReturn);

                                return intToReturn;
                            }
                            else if (memberType == typeof(long))
                            {
                                long longToReturn = 0;

                                long.TryParse(variableDefinition.DefaultValue, out longToReturn);

                                return longToReturn;
                            }
                            else if (memberType == typeof(double))
                            {
                                double doubleToReturn = 0.0;

                                double.TryParse(variableDefinition.DefaultValue, out doubleToReturn);

                                return doubleToReturn;
                            }
                            else
                            {
                                return toReturn;
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        if(memberType.IsEnum && instruction.Value is int)
                        {
                            return Enum.ToObject(memberType, instruction.Value);
                        }
                        else
                        {
                            return instruction.Value;
                        }
                    }
                };

                instanceMember.CustomSetEvent += (owner, value) =>
                {
                    //NamedObjectVariableChangeLogic.ReactToValueSet(instance, typedMember.MemberName, value, out bool makeDefault);

                    //static void ReactToValueSet(NamedObjectSave instance, string memberName, object value, out bool makeDefault)
                    //{
                    // If setting AnimationChianList to null then also null out the CurrentChainName to prevent
                    // runtime errors.
                    //
                    bool makeDefault = false;
                    var ati = instance.GetAssetTypeInfo();
                    var foundVariable = ati?.VariableDefinitions.FirstOrDefault(item => item.Name == typedMember.MemberName);
                    if (foundVariable?.Type == nameof(AnimationChainList))
                    {
                        if (value is string && ((string)value) == "<NONE>")
                        {
                            value = null;
                            makeDefault = true;

                            // Let's also set the CurrentChainName to null
                            GlueCommands.Self.GluxCommands.SetVariableOn(
                                instance,
                                "CurrentChainName",
                                null);
                        }
                    }
                    instanceMember.IsDefault = makeDefault;


                    PerformStandardVariableAssignments(instance, typedMember.MemberName, value);

                    static void PerformStandardVariableAssignments(NamedObjectSave instance, string memberName, object value)
                    {
                        // If we ignore the next refresh, then AnimationChains won't update when the user
                        // picks an AnimationChainList from a combo box:
                        //RefreshLogic.IgnoreNextRefresh();
                        GlueCommands.Self.GluxCommands.SetVariableOn(
                            instance,
                            memberName,
                            value);


                        GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();

                        // let's make the UI faster:

                        // Get this on the UI thread, but use it in the async call below
                        var currentElement = GlueState.Self.CurrentElement;

                        GlueCommands.Self.GluxCommands.SaveGlux();

                        if (currentElement != null)
                        {
                            GlueCommands.Self.GenerateCodeCommands.GenerateElementCodeTask(currentElement);
                        }
                    }





                    instanceMember.IsDefault = makeDefault;
                };

                instanceMember.IsDefaultSet += (owner, args) =>
                {
                    if(instanceMember.IsDefault)
                    {
                        // June 29 2021 - this used to get called whenever
                        // IsDefault is set to either true or false, but we
                        // only want to call MakeDefault if the value is set to true.
                        MakeDefault(instance, typedMember.MemberName);

                    }
                };

                instanceMember.SetValueError += (newValue) =>
                    {
                        if (newValue is string && string.IsNullOrEmpty(newValue as string))
                        {
                            MakeDefault(instance, typedMember.MemberName);
                        }
                    };

                instanceMember.ContextMenuEvents.Add("Tunnel Variable", (not, used) =>
                {
                    string variableToTunnel = null;
                    if (variableDefinition != null)
                    {
                        variableToTunnel = variableDefinition?.Name;
                    }
                    else if(typedMember != null)
                    {
                        variableToTunnel = typedMember.MemberName;
                    }
                    GlueCommands.Self.DialogCommands.ShowAddNewVariableDialog(
                        FlatRedBall.Glue.Controls.CustomVariableType.Tunneled,
                        instance.InstanceName,
                        variableToTunnel);
                });
            }
            return instanceMember;
        }


        /// <summary>
        /// Determines if a variable should be ignored by the variable plugin.
        /// </summary>
        /// <param name="typedMember">The typed member - represents the variable which may be ignored.</param>
        /// <param name="instance">The NamedObjectSave owning the variable.</param>
        /// <param name="ati">The Asset Typ Info for the NamedObjectSave.</param>
        /// <returns>Whether to skip the variable.</returns>
        private static bool GetIfShouldBeSkipped(TypedMemberBase typedMember, NamedObjectSave instance, AssetTypeInfo ati)
        {
            ///////////////////Early Out////////////////////////
            if(typedMember == null)
            {
                return true;
            }

            //////////////////End Early Out//////////////////////


            var name = typedMember.MemberName;

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
            return false;
        }

        private static void MakeDefault(NamedObjectSave instance, string memberName)
        {
            PropertyGridRightClickHelper.SetVariableToDefault(instance, memberName);

            var element = GlueState.Self.CurrentElement;

            // do we want to run this async?
            GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);

            // normally we want to refresh only the variables
            // However, refreshing the variables means "re-assign all variables"
            // If a variable is made default, then re-assigning won't un-assign the
            // already-assigned default value. Therefore, the easiest way to fix this
            // is to just refresh everything. At some point we may want to have the ability
            // to refresh a single variable, but that will be a lot more work.
            //bool sendRefreshCommands = false;
            //GlueCommands.Self.GluxCommands.SaveGlux(sendRefreshCommands);
            //GlueCommands.Self.GlueViewCommands.SendRefreshVariablesCommand();
            GlueCommands.Self.GluxCommands.SaveGlux();

            MainGlueWindow.Self.PropertyGrid.Refresh();
        }


    }
}
