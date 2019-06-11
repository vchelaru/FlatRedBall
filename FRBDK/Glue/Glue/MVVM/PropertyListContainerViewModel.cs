using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Interfaces;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueSaveClasses.Models.TypeConverters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.MVVM
{
    #region SyncedPropertyAttribute Class
    public class SyncedPropertyAttribute : Attribute
    {
        public string OverridingPropertyName { get; set; }
        public Type ConverterType { get; set; }

        public SyncedPropertyAttribute()
        {

        }

        //public SyncedPropertyAttribute(string overridingPropertyName = null, Type converterType)
        //{
        //    OverridingPropertyName = overridingPropertyName;
        //    ConverterType = converterType;
        //}

    }
    #endregion

    public class PropertyListContainerViewModel : ViewModel
    {
        #region Embedded Classes

        public class SyncedPropertyInformation
        {
            public Type Type { get; set; }
            public IConverter Converter { get; set; }
            public string OverridingPropertyName { get; set; }
        }

        #endregion

        public bool PersistChanges { get; set; } = true;

        /// <summary>
        /// The synced properties, where the key is the property name in the view model
        /// </summary>
        Dictionary<string, SyncedPropertyInformation> syncedProperties = new Dictionary<string, SyncedPropertyInformation>();

        IPropertyListContainer glueObject;
        public IPropertyListContainer GlueObject
        {
            get { return glueObject; }
            set
            {
                glueObject = value;
            }
        }

        protected void SetAndPersist<T>(T propertyValue, [CallerMemberName]string propertyName = null)
        {
            var propertyInfo = syncedProperties[propertyName];


            if (base.Set(propertyValue, propertyName) && PersistChanges)
            {
                if(propertyInfo.Converter != null)
                {
                    propertyValue = (T)propertyInfo.Converter.Convert(propertyValue);
                }
                var modelName = propertyInfo.OverridingPropertyName ?? propertyName;

                GlueObject.Properties.SetValue(modelName, propertyValue);

                IElement element = null;
                bool isGlobalContent = false;

                // codegen the object
                if (GlueObject is IElement)
                {
                    element = GlueObject as IElement;
                }
                else if (GlueObject is NamedObjectSave)
                {
                    var nos = GlueObject as NamedObjectSave;

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

                GlueCommands.Self.GluxCommands.SaveGluxTask();
                // save the project


            }
        }

        public PropertyListContainerViewModel()
        {
            Dictionary<Type, IConverter> converterCache = new Dictionary<Type, IConverter>();

            var derivedType = this.GetType();

            var properties = derivedType.GetRuntimeProperties();

            foreach (var property in properties)
            {
                var attributes = property.GetCustomAttributes(true);

                string propertyName = property.Name;

                foreach (var uncastedAttribute in attributes)
                {
                    if (uncastedAttribute is SyncedPropertyAttribute)
                    {
                        var syncedPropertyAttribute = uncastedAttribute as SyncedPropertyAttribute;

                        var information = new SyncedPropertyInformation();
                        information.Type = property.PropertyType;

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

                        information.OverridingPropertyName =
                            syncedPropertyAttribute.OverridingPropertyName;

                        syncedProperties.Add(
                            propertyName,
                            information);

                    }
                }
            }
        }

        public virtual void UpdateFromGlueObject()
        {
            foreach (var kvp in syncedProperties)
            {
                var viewModelPropertyName = kvp.Key;
                var modelPropertyName = kvp.Value.OverridingPropertyName ?? kvp.Key;
                var type = kvp.Value.Type;
                var converter = kvp.Value.Converter;

                var value = GlueObject.Properties.GetValue(modelPropertyName);

                if (type == typeof(float))
                {
                    SetInternal<float>(value, viewModelPropertyName, converter);
                }
                else if (type == typeof(double))
                {
                    SetInternal<double>(value, viewModelPropertyName, converter);

                }
                else if (type == typeof(decimal))
                {
                    SetInternal<decimal>(value, viewModelPropertyName, converter);

                }
                else if (type == typeof(byte))
                {
                    SetInternal<byte>(value, viewModelPropertyName, converter);

                }
                else if (type == typeof(bool))
                {
                    SetInternal<bool>(value, viewModelPropertyName, converter);

                }
                else if (type == typeof(int))
                {
                    SetInternal<int>(value, viewModelPropertyName, converter);

                }
                else if (type == typeof(string))
                {
                    SetInternal<string>(value, viewModelPropertyName, converter);

                }
                else if (type == typeof(char))
                {
                    SetInternal<char>(value, viewModelPropertyName, converter);

                }
                else
                {
                    throw new NotImplementedException();
                }
            }

        }

        private void SetInternal<T>(object toSet, string propertyName, IConverter converter)
        {
            if(toSet == null)
            {
                toSet = default(T);
            }

            if(converter != null)
            {
                toSet = converter.ConvertBack(toSet);
            }

            Set<T>((T)toSet, propertyName);
        }
    }
}
