using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Interfaces;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SetVariable;
using GlueSaveClasses.Models.TypeConverters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.MVVM
{
    #region SyncedPropertyAttribute Class

    /// <summary>
    /// Attribute which indicates that this property is automatically synced with the underlying model. This is used
    /// for ViewModels which represent properties on Glue objects such as NamedObjectSaves. When using this, use SetAndPersist
    /// on the setter.
    /// </summary>
    public class SyncedPropertyAttribute : Attribute
    {
        public string OverridingPropertyName { get; set; }
        public Type ConverterType { get; set; }
        /// <summary>
        /// Property which should be checked when deciding whether to sync the property. If this is null, then the 
        /// property is always synced. If not null, then the property is reflected as a bool and checked whether
        /// to sync this property.
        /// </summary>
        public string SyncingConditionProperty { get; set; }
    }
    #endregion

    /// <summary>
    /// ViewModel-implementing class used to display and persist data for a NamedObjectSave or element. 
    /// </summary>
    /// <example>
    /// var viewModel = new TypeInheritingFromPropertyListContainerViewModel();
    /// viewModel.GlueObject = namedObjectOrElement;
    /// viewModel.UpdateFromGlueObject(); // sets the values according to this
    /// </example>
    public class PropertyListContainerViewModel : ViewModel
    {
        #region Embedded Classes

        public class ViewModelPropertyInformation
        {
            public Type Type { get; set; }
            public IConverter Converter { get; set; }
            public object DefaultValue { get; set; }
            public string OverridingPropertyName { get; set; }
            public bool IsSynced { get; set; }
            public string SyncingConditionProperty { get; set; }

            public override string ToString()
            {
                return $"IsSynced:{IsSynced}";
            }
        }

        #endregion

        #region Fields/Properties

        public bool PersistChanges { get; set; } = true;

        /// <summary>
        /// The synced properties, where the key is the property name in the view model
        /// </summary>
        Dictionary<string, ViewModelPropertyInformation> viewModelProperties = new Dictionary<string, ViewModelPropertyInformation>();

        public IPropertyListContainer GlueObject
        {
            get => Get<IPropertyListContainer>(); 
            set => Set(value); 
        }

        public bool IsUpdatingFromGlueObject { get; set; }

        #endregion

        public PropertyListContainerViewModel()
        {
            Dictionary<Type, IConverter> converterCache = new Dictionary<Type, IConverter>();

            var derivedType = this.GetType();

            var properties = derivedType.GetRuntimeProperties();

            foreach (var property in properties)
            {
                var attributes = property.GetCustomAttributes(true);

                string propertyName = property.Name;

                var information = new ViewModelPropertyInformation();
                information.Type = property.PropertyType;

                viewModelProperties.Add(
                    propertyName,
                    information);

                foreach (var uncastedAttribute in attributes)
                {
                    if(uncastedAttribute is DefaultValueAttribute)
                    {
                        var defaultValueAttribute = uncastedAttribute as DefaultValueAttribute;

                        information.DefaultValue = defaultValueAttribute.Value;
                    }
                    else if (uncastedAttribute is SyncedPropertyAttribute)
                    {
                        var syncedPropertyAttribute = uncastedAttribute as SyncedPropertyAttribute;

                        if (syncedPropertyAttribute.ConverterType != null)
                        {
                            IConverter toAssign = null;
                            if (converterCache.ContainsKey(syncedPropertyAttribute.ConverterType))
                            {
                                toAssign = converterCache[syncedPropertyAttribute.ConverterType];
                            }
                            else
                            {
                                var constructor = syncedPropertyAttribute.ConverterType.GetConstructor(new Type[0]);

                                toAssign = (IConverter)constructor.Invoke(new object[0]);
                                converterCache.Add(syncedPropertyAttribute.ConverterType, toAssign);
                            }
                            information.Converter = toAssign;
                        }

                        information.IsSynced = true;

                        information.OverridingPropertyName =
                            syncedPropertyAttribute.OverridingPropertyName;

                        information.SyncingConditionProperty =
                            syncedPropertyAttribute.SyncingConditionProperty;
                    }
                }
            }
        }

        protected virtual void OnSetAndPersist<T>(T propertyValue, string propertyName = null)
        {

        }

        // made public for reflection
        public bool SetAndPersist<T>(T propertyValue, [CallerMemberName]string propertyName = null, bool forcePersist = false)
        {
            if(viewModelProperties.ContainsKey(propertyName) == false)
            {
                throw new InvalidOperationException($"Did you forget to set the SyncedProperty attribute on {propertyName} in {this.GetType()}?");
            }
            var propertyInfo = viewModelProperties[propertyName];

            T oldValue = Get<T>(propertyName);

            // don't notify the property change yet, do it after setting the value on the Glue
            // object in case whoever listens wants to do codegen or other things depending on the
            // property already being set.
            if ((base.SetWithoutNotifying(propertyValue, propertyName) && PersistChanges) || forcePersist)
            {
                if(propertyInfo.Converter != null)
                {
                    propertyValue = (T)propertyInfo.Converter.Convert(propertyValue);
                }
                var modelName = propertyInfo.OverridingPropertyName ?? propertyName;


                GlueElement element = null;
                bool isGlobalContent = false;

                // codegen the object
                if (GlueObject is GlueElement)
                {
                    element = GlueObject as GlueElement;
                }
                else if (GlueObject is NamedObjectSave nos)
                {
                    element = ObjectFinder.Self.GetElementContaining(nos);
                }
                else if (GlueObject is ReferencedFileSave)
                {
                    var rfs = GlueObject as ReferencedFileSave;

                    element = ObjectFinder.Self.GetElementContaining(rfs);

                    if (element == null)
                    {
                        isGlobalContent = GlueState.Self.CurrentGlueProject.GlobalFiles?.Contains(rfs) == true;
                    }
                }

                //// just in case there's some save/generate running:
                //TaskManager.Self.Add(() =>
                //{

                // DestroyOnUnload cannot use
                // properties, and that is important
                // enough that we need to support it.
                // I could use reflection (which could
                // be slow) or just special case it:

                if(GlueObject == null)
                {
                    throw new InvalidOperationException("Need to set GlueObject before calling SetAndPersist");
                }

                if(GlueObject is ReferencedFileSave && modelName == nameof(ReferencedFileSave.DestroyOnUnload))
                {
                    ((ReferencedFileSave)GlueObject).DestroyOnUnload = (bool)(object)propertyValue;
                }
                else
                {
                    // The default for the type may not match the default value on the view model, so force-set the underlying by calling PersistIfDefault
                    GlueObject.Properties.SetValuePersistIfDefault(modelName, propertyValue);
                }
                //},
                NotifyPropertyChanged(modelName);
                //    "Safely setting property");
                OnSetAndPersist(propertyValue, modelName);



                if(!IsUpdatingFromGlueObject)
                {
                    // February 20, 2023
                    // Generating code and
                    // reacting to Named Object
                    // changed previously was not
                    // surrounded by IsUpdatingFromGlueObject.
                    // But if we are updating from glue object, 
                    // why would we ever generate code or notify
                    // a change has been made? 
                    if (element != null)
                    {
                        TaskManager.Self.Add(() =>
                        {
                            GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
                        }, $"Generating code for {element}", TaskExecutionPreference.AddOrMoveToEnd);
                    }
                    else if (isGlobalContent)
                    {
                        TaskManager.Self.Add(() =>
                        {
                            GlueCommands.Self.GenerateCodeCommands.GenerateGlobalContentCode();
                        }, "Generating Global Content Code", TaskExecutionPreference.AddOrMoveToEnd);
                    }

                    // Do this in a task after the codegen so that the compiler can pick up on it and restart
                    if (GlueObject is NamedObjectSave namedObject)
                    {
                        TaskManager.Self.Add(() =>
                        {
                            TaskManager.Self.OnUiThread(() =>
                                EditorObjects.IoC.Container.Get<NamedObjectSetVariableLogic>().ReactToNamedObjectChangedValue(
                                    propertyName, oldValue, namedObjectSave:namedObject));
                        },
                        "Restarting due to change " + namedObject.InstanceName + "." + propertyName, TaskExecutionPreference.AddOrMoveToEnd);
                    }

                    GlueCommands.Self.GluxCommands.SaveGlux();
                }
                return true;
            }
            return false;
        }

        public bool IsPropertySynced(string propertyName)
        {
            if(viewModelProperties.ContainsKey(propertyName))
            {
                return viewModelProperties[propertyName].IsSynced;
            }
            else
            {
                return false;
            }
        }

        public virtual void UpdateFromGlueObject()
        {
            IsUpdatingFromGlueObject = true;

            var syncedKvps =
                viewModelProperties.Where(item => item.Value.IsSynced).ToArray();

            foreach (var kvp in syncedKvps)
            {
                var viewModelPropertyName = kvp.Key;
                var modelPropertyName = kvp.Value.OverridingPropertyName ?? kvp.Key;
                var type = kvp.Value.Type;
                var converter = kvp.Value.Converter;


                var property = GlueObject.Properties.FirstOrDefault(item => item.Name == modelPropertyName);

                object modelValue = null;

                bool handledByVmDefault = false;

                if (property == null)
                {
                    var defaultVmValue = kvp.Value.DefaultValue;

                    if(defaultVmValue != null)
                    {
                        var method = this.GetType().GetMethod(nameof(SetAndPersist)).MakeGenericMethod(defaultVmValue.GetType());

                        var shouldSync = true;
                        if(!string.IsNullOrEmpty(kvp.Value.SyncingConditionProperty))
                        {
                            shouldSync = base.Get<bool>(kvp.Value.SyncingConditionProperty);
                        }

                        if(shouldSync)
                        {
                            // 3rd parameter forces the persist, because if we're in here, the Glue object does not have this
                            // property
                            method.Invoke(this, new object[] { defaultVmValue, viewModelPropertyName, true });
                        }
                        handledByVmDefault = true;
                    }
                }

                if(handledByVmDefault == false)
                {
                    if(modelPropertyName == nameof(ReferencedFileSave.DestroyOnUnload) && GlueObject is ReferencedFileSave)
                    {
                        // see above to see why we have this special case
                        modelValue = ((ReferencedFileSave)GlueObject).DestroyOnUnload;
                    }
                    else
                    {
                        modelValue = GlueObject.Properties.GetValue(modelPropertyName);
                    }

                    if (type == typeof(float))
                    {
                        if(modelValue is double)
                        {
                            SetInternal<float>( (float)((double)modelValue), viewModelPropertyName, converter);
                        }
                        else
                        {
                            SetInternal<float>(modelValue, viewModelPropertyName, converter);
                        }
                    }
                    else if (type == typeof(double))
                    {
                        SetInternal<double>(modelValue, viewModelPropertyName, converter);

                    }
                    else if (type == typeof(decimal))
                    {
                        SetInternal<decimal>(modelValue, viewModelPropertyName, converter);

                    }
                    else if (type == typeof(byte))
                    {
                        SetInternal<byte>(modelValue, viewModelPropertyName, converter);

                    }
                    else if (type == typeof(bool))
                    {
                        SetInternal<bool>(modelValue, viewModelPropertyName, converter);

                    }
                    else if (type == typeof(int))
                    {
                        if(modelValue is long asLong)
                        {
                            // This can happen due to the JSON conversion
                            SetInternal<int>((int)asLong, viewModelPropertyName, converter);

                        }
                        else
                        {
                            SetInternal<int>(modelValue, viewModelPropertyName, converter);
                        }

                    }
                    else if (type == typeof(string))
                    {
                        SetInternal<string>(modelValue, viewModelPropertyName, converter);

                    }
                    else if (type == typeof(char))
                    {
                        SetInternal<char>(modelValue, viewModelPropertyName, converter);
                    }
                    else
                    {
                        var methods = this.GetType().GetMethods();

                        var method = this.GetType().GetMethod("SetInternal");

                        var genericMethod = method.MakeGenericMethod(type);

                        if(type.IsEnum && modelValue is int valueAsInt)
                        {
                            var castedType = Enum.ToObject(type, valueAsInt);
                            genericMethod.Invoke(this, new object[] { castedType, viewModelPropertyName, converter });
                        }
                        else if (type.IsEnum && modelValue is long valueAsLong)
                        {
                            var castedType = Enum.ToObject(type, valueAsLong);
                            genericMethod.Invoke(this, new object[] { castedType, viewModelPropertyName, converter });
                        }
                        else
                        {
                            genericMethod.Invoke(this, new object[] { modelValue, viewModelPropertyName, converter });
                        }
                    }
                }
            }

            IsUpdatingFromGlueObject = false;

        }

        // made public for reflection
        public void SetInternal<T>(object toSet, string propertyName, IConverter converter)
        {
            if(toSet == null)
            {
                toSet = default(T);
            }

            if(converter != null)
            {
                toSet = converter.ConvertBack(toSet);
            }

            try
            {
                Set<T>((T)toSet, propertyName);
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
