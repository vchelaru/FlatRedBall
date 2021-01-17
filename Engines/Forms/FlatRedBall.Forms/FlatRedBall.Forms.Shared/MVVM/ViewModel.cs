using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace FlatRedBall.Forms.MVVM
{
    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = true)]
    public class DependsOnAttribute : Attribute
    {
        public string ParentProperty { get; set; }

        public DependsOnAttribute(string parentPropertyName)
        {
            ParentProperty = parentPropertyName;
        }

    }

    public class ViewModel : INotifyPropertyChanged
    {
        Dictionary<string, List<string>> notifyRelationships = new Dictionary<string, List<string>>();
        private Dictionary<string, object> propertyDictionary = new Dictionary<string, object>();
        private List<string> dependsOnOwners;

        protected T Get<T>([CallerMemberName]string propertyName = null)
        {
            T toReturn = default(T);

            if (propertyName != null && propertyDictionary.ContainsKey(propertyName))
            {
                try
                {
                    toReturn = (T)propertyDictionary[propertyName];
                }
                catch
                {
                    // if it fails, then just return default T because the type may have changed:
                    toReturn = default(T);
                }
            }

            return toReturn;
        }

        protected bool Set<T>(T propertyValue, [CallerMemberName]string propertyName = null)
        {
            bool didSet = SetWithoutNotifying(propertyValue, propertyName);

            if (didSet)
            {
                NotifyPropertyChanged(propertyName);
            }

            return didSet;
        }

        protected bool SetWithoutNotifying<T>(T propertyValue, [CallerMemberName]string propertyName = null)
        {
            var didSet = false;

            var isDependsOwner = dependsOnOwners?.Contains(propertyName) == true;

            if (propertyDictionary.ContainsKey(propertyName))
            {
                var oldValue = (T)propertyDictionary[propertyName];
                if (EqualityComparer<T>.Default.Equals(oldValue, propertyValue) == false)
                {
                    if(isDependsOwner && oldValue is INotifyPropertyChanged asNotifyPropertyChanged)
                    {
                        asNotifyPropertyChanged.PropertyChanged -= HandleDependsOwnerPropertyChanged;
                    }

                    didSet = true;
                    propertyDictionary[propertyName] = propertyValue;

                    if(isDependsOwner && propertyValue is INotifyPropertyChanged asNotifyPropertyChanged2)
                    {
                        asNotifyPropertyChanged2.PropertyChanged += HandleDependsOwnerPropertyChanged;
                    }
                }
            }
            else
            {
                propertyDictionary[propertyName] = propertyValue;

                // Even though the user is setting a new value, we want to make sure it's
                // not the same:
                var defaultValue = default(T);
                var isSettingDefault =
                    EqualityComparer<T>.Default.Equals(defaultValue, propertyValue);

                didSet = isSettingDefault == false;
                // old value is null, so no need to -= the property changed, but the new one is 
                // potentially not null, so let's += the property changed
                if(isDependsOwner && propertyValue is INotifyPropertyChanged asNotifyPropertyChanged)
                {
                    asNotifyPropertyChanged.PropertyChanged += HandleDependsOwnerPropertyChanged;
                }
            }

            void HandleDependsOwnerPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                NotifyPropertyChanged($"{propertyName}.{e.PropertyName}");
            }

            return didSet;
        }


        public ViewModel()
        {
            var derivedType = this.GetType();

            var properties = derivedType.GetRuntimeProperties();

            foreach (var property in properties)
            {
                var attributes = property.GetCustomAttributes(true);

                string propertyName = property.Name;
                foreach (var uncastedAttribute in attributes)
                {
                    if (uncastedAttribute is DependsOnAttribute)
                    {
                        var attribute = uncastedAttribute as DependsOnAttribute;

                        string parentProperty = attribute.ParentProperty;

                        List<string> childrenProps = null;
                        if (notifyRelationships.ContainsKey(parentProperty) == false)
                        {
                            childrenProps = new List<string>();
                            notifyRelationships[parentProperty] = childrenProps;
                        }
                        else
                        {
                            childrenProps = notifyRelationships[parentProperty];
                        }

                        if(parentProperty.Contains("."))
                        {
                            var owner = parentProperty.Substring(0, parentProperty.IndexOf('.'));

                            if(dependsOnOwners == null)
                            {
                                dependsOnOwners = new List<string>();
                            }

                            if(!dependsOnOwners.Contains(owner))
                            {
                                dependsOnOwners.Add(owner);
                            }
                        }

#if DEBUG
                        if(parentProperty == propertyName)
                        {
                            throw new InvalidOperationException(
                                $"The property {propertyName} should not depend on itself");
                        }
#endif

                        childrenProps.Add(propertyName);
                    }
                }
            }

        }
        protected void ChangeAndNotify<T>(ref T property, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(property, value) == false)
            {
                property = value;
                NotifyPropertyChanged(propertyName);
            }
        }

        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }

            if (notifyRelationships.ContainsKey(propertyName))
            {
                var childPropertyNames = notifyRelationships[propertyName];

                foreach (var childPropertyName in childPropertyNames)
                {
                    // todo - worry about recursive notifications?
                    NotifyPropertyChanged(childPropertyName);
                }
            }

            if(dependsOnOwners?.Contains(propertyName) == true)
            {
                var withDot = propertyName + ".";
                foreach(var relationship in notifyRelationships)
                {
                    if(relationship.Key.StartsWith(withDot))
                    {
                        foreach (var childPropertyName in relationship.Value)
                        {
                            // todo - worry about recursive notifications?
                            NotifyPropertyChanged(childPropertyName);
                        }
                    }
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void ClearPropertyChangedEvents()
        {
            PropertyChanged = null;
        }

        public void SetPropertyChanged(string propertyName, Action action)
        {
            this.PropertyChanged += (sender, args) =>
            {
                if(args.PropertyName == propertyName)
                {
                    action();
                }
            };
        }
    }
}
