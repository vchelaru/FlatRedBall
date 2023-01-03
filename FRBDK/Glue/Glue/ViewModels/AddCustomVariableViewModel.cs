using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.TypeConversions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;

#region Enums

namespace FlatRedBall.Glue.Controls
{
    public enum CustomVariableType
    {
        Exposed,
        Tunneled,
        New
    }
}

#endregion

namespace GlueFormsCore.ViewModels
{
    public class AddCustomVariableViewModel : ViewModel
    {
        #region Top-level Properties

        public GlueElement Element
        {
            get;
            set;
        }

        public CustomVariableType DesiredVariableType
        {
            get => Get<CustomVariableType>();
            set => Set(value);
        }

        [DependsOn(nameof(DesiredVariableType))]
        public bool IsExposedVariableChecked
        {
            get => DesiredVariableType == CustomVariableType.Exposed;
            set
            {
                if (value)
                {
                    DesiredVariableType = CustomVariableType.Exposed;
                }
            }
        }

        [DependsOn(nameof(DesiredVariableType))]
        public bool IsTunneledVariableChecked
        {
            get => DesiredVariableType == CustomVariableType.Tunneled;
            set
            {
                if (value)
                {
                    DesiredVariableType = CustomVariableType.Tunneled;
                }
            }
        }

        [DependsOn(nameof(DesiredVariableType))]
        public bool IsNewVariableChecked
        {
            get => DesiredVariableType == CustomVariableType.New;
            set
            {
                if (value)
                {
                    DesiredVariableType = CustomVariableType.New;
                }
            }
        }

        #endregion

        #region Expose Existing

        [DependsOn(nameof(IsExposedVariableChecked))]
        public Visibility ExposeUiVisibility => IsExposedVariableChecked.ToVisibility();

        public string SelectedExposedVariable
        {
            get => Get<string>();
            set => Set(value);
        }

        public ObservableCollection<string> AvailableExposedVariables
        {
            get => Get<ObservableCollection<string>>();
            set => Set(value);
        }

        #endregion

        #region Tunnel


        [DependsOn(nameof(IsExposedVariableChecked))]
        public Visibility TunnelUiVisibility => IsTunneledVariableChecked.ToVisibility();

        public ObservableCollection<string> AvailableTunneledObjects
        {
            get => Get<ObservableCollection<string>>();
            set => Set(value);
        }

        public string SelectedTunneledObject
        {
            get
            {
                var toReturn = Get<string>();
                if (string.IsNullOrEmpty(toReturn))
                {
                    return null;
                }
                else
                {
                    return toReturn;
                }
            }
            set => Set(value);
        }

        [DependsOn(nameof(SelectedTunneledObject))]
        public List<string> AvailableTunneledVariableNames
        {
            get
            {
                List<string> availableVariables = null;

                NamedObjectSave nos = Element.GetNamedObjectRecursively(SelectedTunneledObject);

                if (nos != null)
                {
                    availableVariables = ExposedVariableManager.GetExposableMembersFor(nos).Select(item => item.Member).ToList();

                    // We should remove any variables that are already tunneled into
                    var elementToUse = Element ?? GlueState.Self.CurrentElement;
                    foreach (CustomVariable customVariable in elementToUse.CustomVariables)
                    {
                        if (customVariable.SourceObject == SelectedTunneledObject)
                        {
                            // Reverse loop since we're removing things
                            for (int i = availableVariables.Count - 1; i > -1; i--)
                            {
                                if (availableVariables[i] == customVariable.SourceObjectProperty)
                                {
                                    availableVariables.RemoveAt(i);
                                    break;
                                }
                            }
                        }
                    }
                }

                if (availableVariables != null)
                {
                    availableVariables.Sort();

                    if (availableVariables != null)
                    {
                        // We don't want to expose things like velocity an acceleration in Glue
                        List<string> velocityAndAccelerationVariables = ExposedVariableManager.GetPositionedObjectRateVariables();
                        // We also don't want to expose relative values - the user just simply sets the value and the state/variable handles
                        // whether it sets relative or absolute depending on whether the Entity is attached or not.
                        // This behavior used to not exist, but users never knew when to use relative or absolute, and
                        // that level of control is not really needed...if it is, custom code can probably handle it.
                        List<string> relativeVariables = ExposedVariableManager.GetPositionedObjectRelativeValues();

                        var availableVariablesCopy = availableVariables.ToArray();
                        foreach (string availableVariable in availableVariablesCopy)
                        {
                            var shouldKeep = !velocityAndAccelerationVariables.Contains(availableVariable) &&
                                !relativeVariables.Contains(availableVariable);

                            if(!shouldKeep)
                            {
                                availableVariables.Remove(availableVariable);
                            }
                        }
                    }
                }
                return availableVariables;
            }
        }

        public string SelectedTunneledVariableName
        {
            get => Get<string>();
            set
            {
                if (Set(value) && !string.IsNullOrEmpty(SelectedTunneledObject) && !string.IsNullOrEmpty(SelectedTunneledVariableName))
                {
                    AlternativeName =
                        SelectedTunneledObject +
                        SelectedTunneledVariableName;
                }
            }
        }

        public string AlternativeName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SelectedOverridingType
        {
            get => Get<string>();
            set => Set(value);
        }

        public ObservableCollection<string> AvailableOverridingTypes
        {
            get => Get<ObservableCollection<string>>();
            set => Set(value);
        }

        public string SelectedTypeConverter
        {
            get => Get<string>();
            set => Set(value);
        }

        public ObservableCollection<string> AvailableTypeConverters
        {
            get => Get<ObservableCollection<string>>();
            set => Set(value);
        }

        #endregion

        #region New Variable

        [DependsOn(nameof(IsExposedVariableChecked))]
        public Visibility NewVariableUiVisibility => IsNewVariableChecked.ToVisibility();

        public bool IsShowStateCategoriesChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string NewVariableName
        {
            get => Get<string>();
            set => Set(value);
        }

        [DependsOn(nameof(IsShowStateCategoriesChecked))]
        public List<string> AvailableNewVariableTypes
        {
            get
            {
                List<string> newVariableTypes = ExposedVariableManager.GetAvailableNewVariableTypes(
                    allowNone: false,
                    includeStateCategories: IsShowStateCategoriesChecked);

                return newVariableTypes;
            }
        }
        public string SelectedNewType
        {
            get => Get<string>();
            set
            {
                if (Set(value) && !CanBeList)
                {
                    IsList = false;
                }
            }
        }

        public bool IsStatic
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool SetByDerived
        {
            get => Get<bool>();
            set => Set(value);
        }


        [DependsOn(nameof(SelectedNewType))]
        public bool CanBeList => SelectedNewType == "string";

        [DependsOn(nameof(CanBeList))]
        public Visibility ListCheckBoxVisibility => CanBeList
            ? Visibility.Visible
            // If we pass Collapsed, the list box will shift around when we make changes to the selection.
            // So we use Hidden
            : Visibility.Hidden;

        public bool IsList
        {
            get => Get<bool>();
            set => Set(value);
        }

        #endregion

        #region Effective (Result) Properties

        public string ResultName
        {
            get
            {
                if (DesiredVariableType == CustomVariableType.Exposed)
                {
                    return SelectedExposedVariable;
                }
                else if (DesiredVariableType == CustomVariableType.Tunneled)
                {
                    return AlternativeName;
                }
                else
                {
                    return NewVariableName;
                }
            }
        }

        public string ResultType
        {
            get
            {
                if (DesiredVariableType == CustomVariableType.Exposed)
                {
                    if (Element is EntitySave asEntitySave)
                    {
                        string type = ExposedVariableManager.GetMemberTypeForEntity(ResultName, asEntitySave);

                        return TypeManager.GetCommonTypeName(type);

                    }
                    else
                    {
                        string type = ExposedVariableManager.GetMemberTypeForScreen(ResultName, Element as ScreenSave);

                        return TypeManager.GetCommonTypeName(type);
                    }
                }
                else if (DesiredVariableType == CustomVariableType.Tunneled)
                {
                    NamedObjectSave nos = Element.GetNamedObjectRecursively(SelectedTunneledObject);
                    string type = ExposedVariableManager.GetMemberTypeForNamedObject(nos, SelectedTunneledVariableName);

                    return TypeManager.GetCommonTypeName(type);
                }
                else
                {
                    return SelectedNewType;
                }
            }
        }

        #endregion

        public AddCustomVariableViewModel(GlueElement glueElement)
        {
            AvailableExposedVariables = new ObservableCollection<string>();

            TypeConverterHelper.InitializeClasses();

            this.Element = glueElement;

            FillExposableVariables();

            FillTunneledValues();

            FillOverridingTunneledTypes();

            FillTunneledTypeConverters();

        }

        #region Fill Lists

        private void FillExposableVariables()
        {
            List<string> availableVariables = null;

            var elementToUse = Element ?? GlueState.Self.CurrentElement;

            if (elementToUse != null)
            {
                availableVariables = ExposedVariableManager.GetExposableMembersFor(elementToUse, true)
                    .Select(item => item.Member)
                    .ToList();

            }

            if (availableVariables != null)
            {
                // We don't want to expose things like velocity an acceleration in Glue
                List<string> velocityAndAccelerationVariables = ExposedVariableManager.GetPositionedObjectRateVariables();
                // We also don't want to expose relative values - the user just simply sets the value and the state/variable handles
                // whether it sets relative or absolute depending on whether the Entity is attached or not.
                // This behavior used to not exist, but users never knew when to use relative or absolute, and
                // that level of control is not really needed...if it is, custom code can probably handle it.
                List<string> relativeVariables = ExposedVariableManager.GetPositionedObjectRelativeValues();

                foreach (string variableName in availableVariables)
                {
                    if (!velocityAndAccelerationVariables.Contains(variableName) && !relativeVariables.Contains(variableName))
                    {
                        AvailableExposedVariables.Add(variableName);
                    }
                }

                if (AvailableExposedVariables.Count > 0)
                {
                    SelectedExposedVariable = AvailableExposedVariables[0];
                }
            }
        }

        public void FillTunneledValues()
        {
            AvailableTunneledObjects = new ObservableCollection<string>();

            var elementTouse = Element;
            List<string> availableObjects = AvailableNamedObjectsAndFiles.GetAvailableObjects(false, true, elementTouse, null);
            foreach (string availableObject in availableObjects)
            {
                this.AvailableTunneledObjects.Add(availableObject);
            }
        }

        private void FillOverridingTunneledTypes()
        {
            AvailableOverridingTypes = new ObservableCollection<string>();
            foreach (string propertyType in ExposedVariableManager.AvailablePrimitives)
            {
                AvailableOverridingTypes.Add(propertyType);
            }

            if (AvailableOverridingTypes.Count > 0)
            {
                SelectedOverridingType = AvailableOverridingTypes[0];
            }
        }

        private void FillTunneledTypeConverters()
        {
            AvailableTypeConverters = new ObservableCollection<string>();

            List<string> converters = AvailableCustomVariableTypeConverters.GetAvailableConverters();

            foreach (string converter in converters)
            {
                AvailableTypeConverters.Add(converter);

            }

            if (AvailableTypeConverters.Count > 0)
            {
                SelectedTypeConverter = AvailableTypeConverters[0];
            }
        }

        #endregion
    }
}
