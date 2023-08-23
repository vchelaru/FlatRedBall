using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace FlatRedBall.Glue.MVVM
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
        protected Dictionary<string, object> propertyDictionary = new Dictionary<string, object>();

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
            if (propertyValue is INotifyCollectionChanged collection)
            {
                var oldValue = Get<T>(propertyName);

                if (oldValue is INotifyCollectionChanged oldCollection)
                {
                    oldCollection.CollectionChanged -= CollectionChangedInternal;
                }
                collection.CollectionChanged += CollectionChangedInternal;
            }

            bool didSet = SetWithoutNotifying(propertyValue, propertyName);

            if (didSet)
            {
                NotifyPropertyChanged(propertyName);
            }

            return didSet;

            void CollectionChangedInternal(object sender, NotifyCollectionChangedEventArgs e)
            {
                NotifyPropertyChanged(propertyName);
            }
        }

        protected bool SetWithoutNotifying<T>(T propertyValue, [CallerMemberName]string propertyName = null)
        {
            var didSet = false;

            if (propertyDictionary.ContainsKey(propertyName))
            {
                var storage = (T)propertyDictionary[propertyName];
                if (EqualityComparer<T>.Default.Equals(storage, propertyValue) == false)
                {
                    didSet = true;
                    propertyDictionary[propertyName] = propertyValue;
                }
            }
            else
            {
                propertyDictionary.Add(propertyName, propertyValue);

                // Even though the user is setting a new value, we want to make sure it's
                // not the same:
                var defaultValue = default(T);
                var isSettingDefault =
                    EqualityComparer<T>.Default.Equals(defaultValue, propertyValue);

                didSet = isSettingDefault == false;
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

                string child = property.Name;
                foreach (var uncastedAttribute in attributes)
                {
                    if (uncastedAttribute is DependsOnAttribute)
                    {
                        var attribute = uncastedAttribute as DependsOnAttribute;

                        string parent = attribute.ParentProperty;

                        List<string> childrenProps = null;
                        if (notifyRelationships.ContainsKey(parent) == false)
                        {
                            childrenProps = new List<string>();
                            notifyRelationships[parent] = childrenProps;
                        }
                        else
                        {
                            childrenProps = notifyRelationships[parent];
                        }

                        childrenProps.Add(child);
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
            if(PropertyChanged != null)
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
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public static class BoolExtensions
    {
        public static System.Windows.Visibility ToVisibility(this bool value)
        {
            if (value)
            {
                return System.Windows.Visibility.Visible;
            }
            else
            {
                return System.Windows.Visibility.Collapsed;
            }
        }
    }
}
