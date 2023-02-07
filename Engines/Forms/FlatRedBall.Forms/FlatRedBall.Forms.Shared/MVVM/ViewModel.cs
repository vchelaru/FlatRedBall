using FlatRedBall.Instructions.Reflection;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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

        public DependsOnAttribute(string owner, string parentPropertyName)
        {
            ParentProperty = owner + "." + parentPropertyName;
        }

        public override string ToString()
        {
            return $"Depends on {ParentProperty}";
        }

    }

    public enum TypeMismatchBehavior
    {
        IgnoreError,
        ThrowException
    }

    public class ViewModel : INotifyPropertyChanged
    {
        Dictionary<string, List<string>> notifyRelationships = new Dictionary<string, List<string>>();
        private Dictionary<string, object> propertyDictionary = new Dictionary<string, object>();
        private List<string> dependsOnOwners;

        public static TypeMismatchBehavior DefaultTypeMismatchBehavior = TypeMismatchBehavior.IgnoreError;

        public int PropertyChangedSubscriptionCount => this.PropertyChanged?.GetInvocationList().Length ?? 0;

        protected T Get<T>([CallerMemberName]string propertyName = null)
        {
            T toReturn = default(T);

            if (propertyName != null && propertyDictionary.ContainsKey(propertyName))
            {
                object uncasted = null;
                try
                {
                    uncasted = propertyDictionary[propertyName];
                    toReturn = (T)uncasted;
                }
                catch(InvalidCastException ex)
                {
                    if (DefaultTypeMismatchBehavior == TypeMismatchBehavior.ThrowException)
                    {
                        throw new InvalidCastException($"The property {propertyName} is of type {typeof(T)} but the inner object is of type {uncasted?.GetType()}");
                    }
                    // if it fails, then just return default T because the type may have changed:
                    toReturn = default(T);
                }
                catch(Exception e)
                {
                    if(DefaultTypeMismatchBehavior == TypeMismatchBehavior.ThrowException)
                    {
                        throw;
                    }
                    // if it fails, then just return default T because the type may have changed:
                    toReturn = default(T);
                }
            }

            return toReturn;
        }

        protected bool Set<T>(T propertyValue, [CallerMemberName]string propertyName = null)
        {
            
            var oldValue = Get<T>(propertyName);

            if(propertyValue is INotifyCollectionChanged collection)
            {
                if(oldValue is INotifyCollectionChanged oldCollection)
                {
                    oldCollection.CollectionChanged -= CollectionChangedInternal;
                }
                collection.CollectionChanged += CollectionChangedInternal;
            }

            bool didSet = SetWithoutNotifying(propertyValue, propertyName, oldValue);

            if (didSet)
            {
                NotifyPropertyChanged(propertyName, oldValue, propertyValue);
            }

            return didSet;

            // Careful, this causes event accumulation. Need to solve this!!
            void CollectionChangedInternal(object sender, NotifyCollectionChangedEventArgs e)
            {
                NotifyPropertyChanged(propertyName);
            }
        }


        protected bool SetWithoutNotifying<T>(T propertyValue, string propertyName, T oldValue)
        {
            var didSet = false;


            if (propertyDictionary.ContainsKey(propertyName))
            {
                if (EqualityComparer<T>.Default.Equals(oldValue, propertyValue) == false)
                {
                    propertyDictionary[propertyName] = propertyValue;
                    didSet = true;
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
                    if (uncastedAttribute is DependsOnAttribute attribute)
                    {
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

        Dictionary<INotifyPropertyChanged, string> ObjectToNameDictionary = new Dictionary<INotifyPropertyChanged, string>();

        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null, object oldValue = null, object newValue = null)
        {
            if (PropertyChanged != null)
            {
                var args = new PropertyChangedExtendedEventArgs(propertyName, oldValue, newValue);
                PropertyChanged(this, args);
            }

            if (notifyRelationships.ContainsKey(propertyName))
            {
                var childPropertyNames = notifyRelationships[propertyName];

                foreach (var childPropertyName in childPropertyNames)
                {
                    // This is going to be on "this" so we can use the old and new values passed, I believe...
                    var newChildPropertyValue = GetValueThroughDictionaryOrReflection(childPropertyName);
                    object oldChildPropertyValue = newValue == newChildPropertyValue
                        // The old value is the actual old value passed on to the parameter, so we can just use that:
                        ? oldValue
                        // we don't know the old value...
                        : null;
                    NotifyPropertyChanged(childPropertyName, oldChildPropertyValue, newChildPropertyValue);
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
                            object newChildValue = null;
                            object oldChildValue = null;
                            // Jan 26 2023 LateBinder is busted... Not sure why, Joel prob doesn't know either...
                            //if (oldValue != null)
                            //{
                            //    oldChildValue = LateBinder.GetValueStatic(oldValue, childPropertyName);
                            //}
                            //if (newValue != null)
                            //{
                            //    newChildValue = LateBinder.GetValueStatic(newValue, childPropertyName);
                            //}

                            NotifyPropertyChanged(childPropertyName, oldChildValue, newChildValue);
                        }
                    }
                }

                #region Internal Methods

                SubscribeToEventsOnNewProperty(newValue, propertyName, oldValue);



                void SubscribeToEventsOnNewProperty<T>(T _newValue, string _propertyName, T _oldValue)
                {
                    var isDependsOwner = dependsOnOwners?.Contains(_propertyName) == true;
                    if (isDependsOwner && _oldValue is INotifyPropertyChanged asNotifyPropertyChanged)
                    {
                        if(ObjectToNameDictionary.ContainsKey(asNotifyPropertyChanged))
                        {
                            ObjectToNameDictionary.Remove(asNotifyPropertyChanged);
                            asNotifyPropertyChanged.PropertyChanged -= HandleDependsOwnerPropertyChanged;
                        }
                    }

                    if (isDependsOwner && _newValue is INotifyPropertyChanged asNotifyPropertyChanged2)
                    {
                        if(ObjectToNameDictionary.ContainsKey(asNotifyPropertyChanged2) == false)
                        {
                            ObjectToNameDictionary[asNotifyPropertyChanged2] = _propertyName;
                            asNotifyPropertyChanged2.PropertyChanged += HandleDependsOwnerPropertyChanged;
                        }
                    }
                }

                #endregion
                // This could have changed based on a different value, so since we now have a new dependensOnOnwer,
                // we should also look at updating this value:

            }
        }

        object GetValueThroughDictionaryOrReflection(string propertyName)
        {
            if(propertyDictionary.ContainsKey(propertyName))
            {
                return propertyDictionary[propertyName];
            }
            else
            {
                var propInfo = this.GetType().GetProperty(propertyName);
                return propInfo?.GetValue(this);
            }
        }

        // This cannot be a local func with closures or else -= will not work.
        // To guarantee that, let's move this out to class scope. Then += will work fine.
        void HandleDependsOwnerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var senderAsNotifyPropertyChanged = sender as INotifyPropertyChanged;
            string senderName = "";
            if (senderAsNotifyPropertyChanged != null && ObjectToNameDictionary.ContainsKey(senderAsNotifyPropertyChanged))
            {
                senderName = ObjectToNameDictionary[senderAsNotifyPropertyChanged];
            }
            if (e is PropertyChangedExtendedEventArgs eExtended)
            {
                NotifyPropertyChanged($"{senderName}.{e.PropertyName}", eExtended.OldValue, eExtended.NewValue);
            }
            else
            {
                NotifyPropertyChanged($"{senderName}.{e.PropertyName}");
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

    // From https://stackoverflow.com/questions/47723876/how-to-capture-old-value-and-new-value-in-inotifypropertychanged-implementation#:~:text=NotifyPropertyChanged%20event%20where%20event%20args%20contain%20the%20old,public%20delegate%20void%20PropertyChangedExtendedEventHandler%20%28object%20sender%2C%20PropertyChangedExtendedEventArgs%20e%29%3B
    public class PropertyChangedExtendedEventArgs : PropertyChangedEventArgs
    {
        public virtual object OldValue { get; private set; }
        public virtual object NewValue { get; private set; }

        public PropertyChangedExtendedEventArgs( string propertyName, object oldValue,
               object newValue)
               : base(propertyName)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

}
