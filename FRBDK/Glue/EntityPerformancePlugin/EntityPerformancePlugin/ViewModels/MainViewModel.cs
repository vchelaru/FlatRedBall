using EntityPerformancePlugin.Enums;
using FlatRedBall;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace EntityPerformancePlugin.ViewModels
{


    public class MainViewModel : ViewModel
    {
        List<object> objectsBlockingChanges = new List<object>();
        bool RaiseChangeEvents => objectsBlockingChanges.Count == 0;

        #region Selection-specific properties

        public PropertyManagementMode SelectedPropertyManagementMode
        {
            get { return Get<PropertyManagementMode>(); }
            set
            {
                if(Set(value))
                {
                    NotifyPropertyChanged(nameof(IsSelectionFullyManaged));
                    NotifyPropertyChanged(nameof(IsSelectionSelectingManagedProperties));
                }
            }
        }


        [DependsOn(nameof(IsRootSelected))]
        [DependsOn(nameof(SelectedInstance))]
        [DependsOn(nameof(SelectedItemProperties))]
        public string RightSideMessage
        {
            get
            {
                if (IsRootSelected == false && SelectedInstance == null)
                {
                    return "Select the entity or instance in the tree view to see available properties";
                }
                else if (IsRootSelected)
                {
                    return null;
                }
                else// if(SelectedInstance != null)
                {
                    if (SelectedItemProperties.Any())
                    {
                        return null;
                    }
                    else
                    {
                        return "The selected instance does not support management through this plugin";
                    }
                }
            }
        }


        [DependsOn(nameof(RightSideMessage))]
        public Visibility PropertyListVisibility
        {
            get
            {
                if(RightSideMessage == null)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        [DependsOn(nameof(PropertyListVisibility))]
        public Visibility RightSideMessageVisibility
        {
            get
            {
                if(PropertyListVisibility == Visibility.Visible)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }

        public bool IsRootSelected
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        public bool IsSelectionFullyManaged
        {
            get { return SelectedPropertyManagementMode == PropertyManagementMode.FullyManaged; }
            set
            {
                if(value != IsSelectionFullyManaged)
                {
                    if (value)
                    {
                        SelectedPropertyManagementMode = PropertyManagementMode.FullyManaged;
                    }
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(IsSelectionSelectingManagedProperties));

                }
            }
        }

        public bool IsSelectionSelectingManagedProperties
        {
            get { return SelectedPropertyManagementMode == PropertyManagementMode.SelectManagedProperties; }
            set
            {
                if(value != IsSelectionSelectingManagedProperties)
                {
                    if(value)
                    {
                        SelectedPropertyManagementMode = PropertyManagementMode.SelectManagedProperties;
                    }
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(IsSelectionFullyManaged));
                }
            }
        }

        ObservableCollection<VelocityPropertyViewModel> selectedItemProperties = new ObservableCollection<VelocityPropertyViewModel>();
        public ObservableCollection<VelocityPropertyViewModel> SelectedItemProperties
        {
            get
            {
                return selectedItemProperties;
            }
        }

        public InstanceViewModel SelectedInstance
        {
            get { return Get<InstanceViewModel> (); }
            set { Set(value); }
        }

        #endregion

        public EntitySave Entity
        {
            get; set;
        }

        public string EntityName
        {
            get { return Entity.Name; }
        }

        public PropertyManagementMode EntityManagementMode
        {
            get { return Get<PropertyManagementMode>(); }
            set { Set(value); }
        }
        public ObservableCollection<string> EntityManagedProperties
        {
            get { return Get<ObservableCollection<string>>(); }
            set { Set(value); }
        }

        public ObservableCollection<InstanceViewModel> Instances
        {
            get { return Get<ObservableCollection<InstanceViewModel>>(); }
            set { Set(value); }
        }

        public event Action<string> AnyValueChanged;


        public MainViewModel()
        {
            EntityManagedProperties = new ObservableCollection<string>();

            Instances = new ObservableCollection<InstanceViewModel>();

            this.PropertyChanged += HandlePropertyChanged;
        }

        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var changedSelection = false;
            var isPropertyBroadcastedExternally = false;

            if (e.PropertyName == nameof(SelectedInstance))
            {
                var blocker = new object();
                objectsBlockingChanges.Add(blocker);
                changedSelection = true;
                if (SelectedInstance != null)
                {
                    selectedItemProperties.Clear();
                    AddPropertiesForType(selectedItemProperties, SelectedInstance.Type);

                    // Check any properties that are checked by the instance
                    foreach(var selectedProperty in SelectedInstance.SelectedProperties)
                    {
                        var propertyViewModel = SelectedItemProperties.FirstOrDefault(item => item.Name == selectedProperty);
                        if(propertyViewModel != null)
                        {
                            propertyViewModel.IsChecked = true;
                        }
                    }

                    SelectedPropertyManagementMode = SelectedInstance.PropertyManagementMode;
                }
                else
                {
                    // If we notify this, then the json gets saved. We don't want that
                    SelectedPropertyManagementMode = EntityManagementMode;

                }

                objectsBlockingChanges.Remove(blocker);
            }
            else if (e.PropertyName == nameof(IsRootSelected))
            {
                // If it's set to true, make sure no instance is selected
                SelectedInstance = null;

                var blocker = new object();
                objectsBlockingChanges.Add(blocker);
                changedSelection = true;
                if(IsRootSelected)
                {
                    selectedItemProperties.Clear();

                    string type = GetFrbTypeForEntity(Entity);

                    AddPropertiesForType(selectedItemProperties, type);

                    foreach (var selectedProperty in this.EntityManagedProperties)
                    {
                        var propertyViewModel = SelectedItemProperties.FirstOrDefault(item => item.Name == selectedProperty);
                        if (propertyViewModel != null)
                        {
                            propertyViewModel.IsChecked = true;
                        }
                    }

                    SelectedPropertyManagementMode = EntityManagementMode;
                }
                objectsBlockingChanges.Remove(blocker);
            }
            else if(e.PropertyName == nameof(SelectedPropertyManagementMode))
            {
                isPropertyBroadcastedExternally = true;
                if(SelectedInstance != null)
                {
                    SelectedInstance.PropertyManagementMode = SelectedPropertyManagementMode;
                }
                else
                {
                    EntityManagementMode = SelectedPropertyManagementMode;
                }
            }

            if(changedSelection && IsRootSelected == false && SelectedInstance == null)
            {
                selectedItemProperties.Clear();
            }

            if(!changedSelection && RaiseChangeEvents && isPropertyBroadcastedExternally)
            {
                AnyValueChanged?.Invoke(e.PropertyName);
            }
        }

        private static string GetFrbTypeForEntity(EntitySave entity)
        {
            if(!string.IsNullOrEmpty(entity.BaseEntity))
            {
                if(entity.BaseEntity == nameof(FlatRedBall.Sprite) ||
                    entity.BaseEntity == "FlatRedBall.Sprite")
                {
                    return nameof(FlatRedBall.Sprite);
                }
                if (entity.BaseEntity == nameof(FlatRedBall.Graphics.Text) ||
                    entity.BaseEntity == "FlatRedBall.Graphics.Text")
                {
                    return nameof(FlatRedBall.Graphics.Text);
                }
                else
                {
                    var baseEntity = FlatRedBall.Glue.Elements.ObjectFinder.Self.GetEntitySave(entity.BaseEntity);

                    if(baseEntity != null)
                    {
                        return GetFrbTypeForEntity(baseEntity);
                    }
                    else
                    {
                        return nameof(PositionedObject);
                    }
                }
            }
            else
            {
                return nameof(PositionedObject);
            }
        }

        #region Add Properties by type methods

        private void AddPropertiesForType(ObservableCollection<VelocityPropertyViewModel> selectedItemProperties, string type)
        {
            var handled = false;
            switch(type)
            {
                case "Sprite":
                    AddPositionedObjectProperties(selectedItemProperties);
                    AddIColorableProperties(selectedItemProperties);
                    AddIAnimationChainAnimatableProperties(selectedItemProperties);
                    AddIScalableProperties(selectedItemProperties);
                    handled = true;
                    break;

                case "Text":
                    AddPositionedObjectProperties(selectedItemProperties);
                    AddIColorableProperties(selectedItemProperties);
                    AddTextProperties(selectedItemProperties);
                    handled = true;
                    break;
                    // Shapes cannot currently be managed by this plugin. Not sure
                    // if we'll ever add that functionality.
                case "Circle":
                    //AddPositionedObjectProperties(selectedItemProperties);
                    //AddCircleProperties(selectedItemProperties);
                    handled = true;
                    break;
                case "AxisAlignedRectangle":
                    //AddPositionedObjectProperties(selectedItemProperties);
                    //AddIScalableProperties(selectedItemProperties);
                    handled = true;
                    break;
                case "Polygon":
                    //AddPositionedObjectProperties(selectedItemProperties);
                    handled = true;
                    break;
                case nameof(PositionedObject):
                    AddPositionedObjectProperties(selectedItemProperties);
                    handled = true;
                    break;
            }

            foreach(var property in selectedItemProperties)
            {
                property.PropertyChanged += HandlePropertyChecked;
            }
        }

        private void HandlePropertyChecked(object sender, PropertyChangedEventArgs e)
        {
            var senderAsViewModel = (VelocityPropertyViewModel)sender;
            string changedVariable = null;
            switch(e.PropertyName)
            {
                case nameof(VelocityPropertyViewModel.IsChecked):
                    changedVariable = senderAsViewModel.Name;
                    break;
            }

            bool shouldRaiseValueChanged = false;

            if(changedVariable != null)
            {
                if(IsRootSelected)
                {
                    var isAlreadyInList = this.EntityManagedProperties.Contains(senderAsViewModel.Name);

                    if (senderAsViewModel.IsChecked && isAlreadyInList == false)
                    {
                        this.EntityManagedProperties.Add(senderAsViewModel.Name);
                        shouldRaiseValueChanged = true;
                    }
                    else if (senderAsViewModel.IsChecked == false && isAlreadyInList)
                    {
                        this.EntityManagedProperties.Remove(senderAsViewModel.Name);
                        shouldRaiseValueChanged = true;
                    }
                }
                else
                {
                    var instanceList = this.Instances.FirstOrDefault(item => item.Name == SelectedInstance?.Name)?.SelectedProperties;

                    if(instanceList != null)
                    {
                        var isAlreadyInList = instanceList.Contains(senderAsViewModel.Name);
                        if (senderAsViewModel.IsChecked && isAlreadyInList == false)
                        {
                            instanceList.Add(senderAsViewModel.Name);
                            shouldRaiseValueChanged = true;
                        }
                        else if(senderAsViewModel.IsChecked == false && isAlreadyInList)
                        {
                            instanceList.Remove(senderAsViewModel.Name);
                            shouldRaiseValueChanged = true;
                        }
                    }
                }

                if(shouldRaiseValueChanged)
                {
                    AnyValueChanged?.Invoke(e.PropertyName);
                }
            }
        }

        private void AddCircleProperties(ObservableCollection<VelocityPropertyViewModel> selectedItemProperties)
        {
            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(Circle.RadiusVelocity) });
        }

        private void AddTextProperties(ObservableCollection<VelocityPropertyViewModel> selectedItemProperties)
        {
            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(Text.ScaleVelocity) });
            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(Text.SpacingVelocity) });

        }

        private void AddIScalableProperties(ObservableCollection<VelocityPropertyViewModel> selectedItemProperties)
        {
            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(IScalable.ScaleXVelocity) });
            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(IScalable.ScaleYVelocity) });

        }

        private static void AddIAnimationChainAnimatableProperties(ObservableCollection<VelocityPropertyViewModel> selectedItemProperties)
        {
            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(IAnimationChainAnimatable.Animate) });
        }

        private static void AddIColorableProperties(ObservableCollection<VelocityPropertyViewModel> selectedItemProperties)
        {
            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(IColorable.AlphaRate) });
            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(IColorable.RedRate) });
            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(IColorable.GreenRate) });
            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(IColorable.BlueRate) });
        }

        private static void AddPositionedObjectProperties(ObservableCollection<VelocityPropertyViewModel> selectedItemProperties)
        {
            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(PositionedObject.XVelocity) });
            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(PositionedObject.YVelocity) });
            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(PositionedObject.ZVelocity) });

            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(PositionedObject.Drag) });

            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(PositionedObject.XAcceleration) });
            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(PositionedObject.YAcceleration) });
            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(PositionedObject.ZAcceleration) });

            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = "Attachment" });

            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = "Instruction Execution" });


            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(PositionedObject.RelativeXVelocity) });
            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(PositionedObject.RelativeYVelocity) });
            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(PositionedObject.RelativeZVelocity) });

            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(PositionedObject.RelativeXAcceleration) });
            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(PositionedObject.RelativeYAcceleration) });
            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(PositionedObject.RelativeZAcceleration) });




            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(PositionedObject.RotationXVelocity) });
            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(PositionedObject.RotationYVelocity) });
            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(PositionedObject.RotationZVelocity) });


            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(PositionedObject.RelativeRotationXVelocity) });
            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(PositionedObject.RelativeRotationYVelocity) });
            selectedItemProperties.Add(new VelocityPropertyViewModel { Name = nameof(PositionedObject.RelativeRotationZVelocity) });
        }

        #endregion

        internal void Clear()
        {
            Instances.Clear();
            Entity = null;
        }

        internal void UpdateTo(EntitySave entitySave)
        {
            Instances.Clear();
            Entity = entitySave;

            var allObjects = entitySave.AllNamedObjects;
            foreach (var item in allObjects)
            {
                var instanceViewModel = new InstanceViewModel();
                instanceViewModel.Name = item.InstanceName;
                instanceViewModel.Type = item.InstanceType;

                Instances.Add(instanceViewModel);
            }

            AssignPropertyChangedEventsOnInstanceViewModels();
        }

        public void AssignPropertyChangedEventsOnInstanceViewModels()
        {
            foreach (var instanceViewModel in Instances)
            {
                instanceViewModel.PropertyChanged += (sender, args) => AnyValueChanged?.Invoke(args.PropertyName);
            }
        }
    }
}
