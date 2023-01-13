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

namespace OfficialPlugins.VariableDisplay
{
    static class NamedObjectVariableShowingLogic
    {
        #region Create InstanceMember (Variable)
        private static InstanceMember CreateInstanceMember(NamedObjectSave instance,
            GlueElement container,
            string memberName,
            Type memberType,
            string customTypeName,
            AssetTypeInfo ati,
            VariableDefinition variableDefinition, IEnumerable<MemberCategory> categories)
        {
            bool shouldBeSkipped = 
                GetIfShouldBeSkipped(memberName, instance, ati);
            ///////Early Out//////////
            if (shouldBeSkipped)
            {
                return null;
            }
            ////End Early Out///////

            DataGridItem instanceMember = null;

            #region Property Displayer/forced options


            EntitySave nosEntity = instance.SourceType == SourceType.Entity
                ? ObjectFinder.Self.GetEntitySave(instance.SourceClassType)
                : null;

            var variableInNos = nosEntity?.GetCustomVariableRecursively(memberName);
            CustomVariable baseNos = variableInNos != null
                ? ObjectFinder.Self.GetBaseCustomVariable(variableInNos)
                : null ;

            var isSharedStatic = baseNos?.IsShared == true;

            /////////////////Early Out///////////////////////////
            if(isSharedStatic)
            {
                return null;
            }
            ///////////////End Early Out/////////////////////////

            TypeConverter typeConverter = GetTypeConverter(instance, container, memberName, memberType, customTypeName, variableDefinition);

            bool isObjectInFile = typeConverter is IObjectsInFileConverter;

            if (isObjectInFile)
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

            if (variableDefinition?.PreferredDisplayer != null)
            {
                instanceMember.PreferredDisplayer = variableDefinition.PreferredDisplayer;

                if (instanceMember.PreferredDisplayer == typeof(SliderDisplay) && variableDefinition.MinValue != null && variableDefinition.MaxValue != null)
                {
                    instanceMember.PropertiesToSetOnDisplayer[nameof(SliderDisplay.MaxValue)] =
                        variableDefinition.MaxValue.Value;
                    instanceMember.PropertiesToSetOnDisplayer[nameof(SliderDisplay.MinValue)] =
                        variableDefinition.MinValue.Value;
                }

                foreach (var item in variableDefinition.PropertiesToSetOnDisplayer)
                {
                    instanceMember.PropertiesToSetOnDisplayer[item.Key] = item.Value;
                }

            }
            else if (variableDefinition?.Name == nameof(FlatRedBall.PositionedObject.RotationZ) && variableDefinition.Type == "float")
            {
                instanceMember.PreferredDisplayer = typeof(AngleSelectorDisplay);
            }
            else if (variableDefinition?.MinValue != null && variableDefinition?.MaxValue != null)
            {
                instanceMember.PreferredDisplayer = typeof(SliderDisplay);
                instanceMember.PropertiesToSetOnDisplayer[nameof(SliderDisplay.MaxValue)] =
                    variableDefinition.MaxValue.Value;
                instanceMember.PropertiesToSetOnDisplayer[nameof(SliderDisplay.MinValue)] =
                    variableDefinition.MinValue.Value;
            }

            if(instanceMember.PreferredDisplayer == typeof(AngleSelectorDisplay))
            {
                instanceMember.PropertiesToSetOnDisplayer[nameof(AngleSelectorDisplay.TypeToPushToInstance)] =
                    AngleType.Radians;

                // this used to be 1, then 5, but 10 is prob enough resolution. Numbers can be typed.
                // 15 is better, gives the user access to 45
                instanceMember.PropertiesToSetOnDisplayer[nameof(AngleSelectorDisplay.SnappingInterval)] =
                    15m;
            }

            #endregion

            instanceMember.FirstGridLength = new System.Windows.GridLength(140);

            instanceMember.UnmodifiedVariableName = memberName;
            string displayName = StringFunctions.InsertSpacesInCamelCaseString(memberName);
            instanceMember.DisplayName = displayName;


            // hack! Certain ColorOperations aren't supported in MonoGame. One day they will be if we ever get the
            // shader situation solved. But until then, these cause crashes so let's remove them.
            // Do this after setting the type converter
            if (variableDefinition?.Type == nameof(FlatRedBall.Graphics.ColorOperation))
            {
                instanceMember.TypeConverter = null;
                // one day?
                instanceMember.CustomOptions = new List<object>();
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
            else
            {
                instanceMember.TypeConverter = typeConverter;
            }

            #region CustomGetTypeEvent
            instanceMember.CustomGetTypeEvent += (throwaway) => memberType;
            #endregion

            #region CustomGet

            AssignCustomGetEvent(instance, container, memberName, memberType, variableDefinition, instanceMember);

            #endregion

            #region CustomSetEvent

            instanceMember.CustomSetEvent += async (owner, value) =>
            {
                await HandleVariableSet(variableDefinition, container, instance, memberName, value, instanceMember,
                    categories);
            };

            #endregion

            #region IsDefaultSet

            instanceMember.IsDefault = instance.GetCustomVariable(memberName) == null;

            instanceMember.IsDefaultSet += (owner, args) =>
            {
                if (instanceMember.IsDefault)
                {
                    // June 29 2021 - this used to get called whenever
                    // IsDefault is set to either true or false, but we
                    // only want to call MakeDefault if the value is set to true.
                    MakeDefault(instance, memberName);

                }
            };

            #endregion

            #region SetValueError

            instanceMember.SetValueError += (newValue) =>
            {
                if (newValue is string && string.IsNullOrEmpty(newValue as string))
                {
                    MakeDefault(instance, memberName);
                }
            };

            #endregion

            AddContextMenuEvents(instance, container, memberName, variableDefinition, instanceMember);

            return instanceMember;
        }

        #endregion

        #region Get Variable Value
        private static void AssignCustomGetEvent(NamedObjectSave instance, GlueElement container,
            string memberName, Type memberType, VariableDefinition variableDefinition, DataGridItem instanceMember)
        {
            if (variableDefinition.CustomVariableGet != null)
            {
                instanceMember.CustomGetEvent += (throwaway) =>
                {
                    return variableDefinition.CustomVariableGet(container, instance, memberName);
                };
            }
            else
            {
                instanceMember.CustomGetEvent += (throwaway) =>
                {
                    return ObjectFinder.Self.GetValueRecursively(instance, container, memberName, memberType, variableDefinition);
                };
            }
        }
        #endregion

        #region Set Variable Value

        private static async Task HandleVariableSet(VariableDefinition variableDefinition, GlueElement container, 
            NamedObjectSave instance, string memberName, object value, DataGridItem instanceMember,
            IEnumerable<MemberCategory> categories)
        {
            if (GlueState.Self.CurrentGlueProject == null)
                return;
            //NamedObjectVariableChangeLogic.ReactToValueSet(instance, memberName, value, out bool makeDefault);

            //static void ReactToValueSet(NamedObjectSave instance, string memberName, object value, out bool makeDefault)
            //{
            // If setting AnimationChianList to null then also null out the CurrentChainName to prevent
            // runtime errors.
            //

            if (variableDefinition.CustomVariableSet != null)
            {
                variableDefinition.CustomVariableSet(container, instance, memberName, value);
            }
            else
            {
                bool makeDefault = false;
                var ati = instance.GetAssetTypeInfo();
                var foundVariable = ati?.VariableDefinitions.FirstOrDefault(item => item.Name == memberName);
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

                // If we ignore the next refresh, then AnimationChains won't update when the user
                // picks an AnimationChainList from a combo box:
                //RefreshLogic.IgnoreNextRefresh();

                // Discussion about SetVariableOn vs SetVariableOnAsync:
                // SetVariableOn happens immediately - it does not respect
                // the task system. SetVariableOnAsync does use the task system,
                // which is safer, since setting the value immediately can cause bugs
                // due to variables changing while other tasks are running. However, if
                // SetVariableOnAsync is used, then that means the logic for setting the
                // variable will not run until the TaskManager gets to this task. If there
                // are other tasks running, then that means the variable will not get set right
                // away. This can cause the property grid to display the old value after the user
                // presses ENTER. Therefore, for now we need to use the obsolete SetVariableOn, and 
                // think of a more sophisticated solution.
                GlueCommands.Self.GluxCommands.SetVariableOn(
                instance,
                    memberName,
                    value, performSaveAndGenerateCode: false, updateUi: false);


                // We're going to delay updating all UI, saving, and codegen for a half second to not spam the system:
                await System.Threading.Tasks.Task.Delay(400);

                // Set subtext before refreshing property grid
                AssignVariableSubtext(instance, categories.ToList(), instance.GetAssetTypeInfo());

                instanceMember.IsDefault = makeDefault;

                await TaskManager.Self.AddAsync(async () =>
                {
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(container);
                    EditorObjects.IoC.Container.Get<GlueErrorManager>().ClearFixedErrors();

                    GlueCommands.Self.DoOnUiThread(() =>
                    {
                        MainGlueWindow.Self.PropertyGrid.Refresh();
                        PropertyGridHelper.UpdateNamedObjectDisplay();
                        if (instanceMember.DisplayName == "Name")
                        {
                            GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(container,
                                // We can be faster by doing only a NamedObject refresh, since the only way this could change is the Name...right?
                                FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces.TreeNodeRefreshType.NamedObjects);
                        }
                    });

                    if (GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.SeparateJsonFilesForElements)
                    {
                        await GlueCommands.Self.GluxCommands.SaveElementAsync(container);
                    }
                    else
                    {
                        GlueCommands.Self.GluxCommands.SaveGlux(TaskExecutionPreference.AddOrMoveToEnd);
                    }


                }, $"Delayed task to do all updates for {instance}", TaskExecutionPreference.AddOrMoveToEnd);

            }
        }

        #endregion

        private static TypeConverter GetTypeConverter(NamedObjectSave instance, GlueElement container, string memberName, Type memberType, string customTypeName,
            VariableDefinition variableDefinition)
        {
            var toReturn = PluginManager.GetTypeConverter(
                 container, instance, memberType, memberName, customTypeName);

            if (variableDefinition?.ForcedOptions?.Count > 0)
            {
                var converter = new DelegateBasedTypeConverter();
                converter.CustomDelegate = () =>
                {
                    var list = new List<string>();
                    list.AddRange(variableDefinition.ForcedOptions);
                    return list;
                };
                return converter;
            }
            else if (variableDefinition?.CustomGetForcedOptionFunc != null)
            {
                var converter = new DelegateBasedTypeConverter();
                converter.CustomDelegate = () =>
                {
                    var list = new List<string>();
                    list.AddRange(variableDefinition.CustomGetForcedOptionFunc(container, instance, null));
                    return list;
                };
                return converter;
            }

            return toReturn;
        }

        private static void CreateCategoriesAndVariables(NamedObjectSave instance, GlueElement container,
            List<MemberCategory> categories, AssetTypeInfo ati)
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
                        InstanceMember instanceMember = CreateInstanceMember(instance, container, variableName, type, typedMember.CustomTypeName, ati, variableDefinition, categories);
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

        public static void UpdateShownVariables(DataUiGrid grid, NamedObjectSave instance, IElement container,
            AssetTypeInfo assetTypeInfo = null)
        {
            #region Initial logic

            grid.Categories.Clear();

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

            SetAlternatingColors(grid, categories);

            foreach (var category in categories)
            {
                grid.Categories.Add(category);
            }

            grid.Refresh();
        }

        private static MemberCategory CreateTopmostCategory(List<MemberCategory> categories)
        {
            MemberCategory topmostCategory = new MemberCategory();
            topmostCategory.Name = "";
            topmostCategory.HideHeader = true;
            categories.Insert(0, topmostCategory);
            return topmostCategory;
        }

        private static void AssignVariableSubtext(NamedObjectSave instance, List<MemberCategory> categories, AssetTypeInfo assetTypeInfo)
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
            InstanceMember instanceMember = CreateInstanceMember(instance, container, typedMember.MemberName, typedMember.MemberType, typedMember.CustomTypeName, ati, variableDefinition, categories);

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

        private static void AddContextMenuEvents(NamedObjectSave instance, GlueElement container, string memberName, VariableDefinition variableDefinition, DataGridItem instanceMember)
        {
            var isAlreadyTunneled = container.CustomVariables.Any(item =>
                item.SourceObject == instance.InstanceName && item.SourceObjectProperty == memberName);

            if (!isAlreadyTunneled)
            {
                instanceMember.ContextMenuEvents.Add("Tunnel Variable...", (not, used) =>
                {
                    string variableToTunnel = null;
                    if (variableDefinition != null)
                    {
                        variableToTunnel = variableDefinition?.Name;
                    }
                    else if (!string.IsNullOrWhiteSpace(memberName))
                    {
                        variableToTunnel = memberName;
                    }
                    GlueCommands.Self.DialogCommands.ShowAddNewVariableDialog(
                        FlatRedBall.Glue.Controls.CustomVariableType.Tunneled,
                        instance.InstanceName,
                        variableToTunnel);
                });

                instanceMember.ContextMenuEvents[$"Tunnel as {instance.InstanceName}{memberName}"] = (not, used) =>
                {
                    //GlueCommands.Self.DialogCommands.ShowAddNewVariableDialog();
                    CustomVariable newVariable = new CustomVariable();
                    newVariable.Name = instance.InstanceName + memberName;
                    newVariable.Type = variableDefinition.Type;
                    newVariable.SourceObject = instance.InstanceName;
                    newVariable.SourceObjectProperty = memberName;

                    newVariable.Category = variableDefinition?.Category;

                    GlueCommands.Self.GluxCommands.ElementCommands.AddCustomVariableToElement(newVariable, container);

                };
            }
        }


        /// <summary>
        /// Determines if a variable should be ignored by the variable plugin.
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

            PluginManager.ReactToChangedProperty(memberName, oldValue, element, new PluginManager.NamedObjectSaveVariableChange
            { 
                NamedObjectSave = instance,
                ChangedMember = memberName
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
