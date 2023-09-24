using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Utilities;
using Localization;
using Microsoft.Build.Framework;

namespace FlatRedBall.Glue.ViewModels
{
    #region Selected Item Wrapper class

    public class SelectedItemWrapper
    {
        public object BackingObject { get; set; }

        public event Action StrongSelect;

        public string MainText
        {
            get
            {
                if (BackingObject is string) return BackingObject.ToString();
                else if (BackingObject is AssetTypeInfo ati) return ati.FriendlyName;
                //else if (BackingObject is EntitySave entity) return entity.Name.Substring("Entities/".Length);
                else if (BackingObject is EntitySave entity) return entity.Name;
                else if (BackingObject is ReferencedFileSave rfs) return rfs.Name;
                else return null;
            }
        }

        public string SubText
        {
            get
            {
                if (BackingObject is AssetTypeInfo ati) return $"({ati.QualifiedRuntimeTypeName.QualifiedType})";
                //else if (BackingObject is EntitySave entity) return entity.Name.Substring("Entities/".Length);
                //else if (BackingObject is ReferencedFileSave rfs) return rfs.Name;
                else return null;
            }
        }

        public Visibility SubtextVisibility => string.IsNullOrEmpty(SubText)
            ? Visibility.Collapsed
            : Visibility.Visible;

        public ICommand HandleStrongSelect { get; private set; }

        public SelectedItemWrapper()
        {
            HandleStrongSelect = new Command(
                () => StrongSelect());
        }

        public override string ToString()
        {
            return MainText;
        }
    }

    #endregion

    public class AddObjectViewModel : ViewModel
    {
        #region SourceType

        public SourceType SourceType 
        {
            get => Get<SourceType>();
            set
            {
                if (base.SetWithoutNotifying(value))
                {
                    ForceRefreshToSourceType();
                    RefreshDefaultIsCallActivityChecked();
                }
            }
        }

        public void ForceRefreshToSourceType()
        {
            RefreshAllSelectedItems();

            RefreshFilteredItems();

            NotifyPropertyChanged(nameof(SourceType));

            SelectIfNoSelection();
        }

        public bool IsFlatRedBallType
        {
            get => SourceType == SourceType.FlatRedBallType;
            set
            {
                if(value)
                {
                    SourceType = SourceType.FlatRedBallType;
                }
            }
        }

        public bool IsEntityType
        {
            get => SourceType == SourceType.Entity;
            set
            {
                if (value)
                {
                    SourceType = SourceType.Entity;
                }
            }
        }

        public bool IsFromFileType
        {
            get => SourceType == SourceType.File;
            set
            {
                if (value)
                {
                    SourceType = SourceType.File;
                }
            }
        }

        public bool IsGumType
        {
            get => SourceType == SourceType.Gum;
            set
            {
                if(value)
                {
                    SourceType = SourceType.Gum;
                }
            }
        }

        #endregion

        #region Filtering

        public string FilterText
        {
            get => Get<string>();
            set
            {
                if (Set(value))
                {
                    RefreshFilteredItems();
                    SelectIfNoSelection();
                }
            }
        }

        List<T> Filter<T>(IEnumerable<T> allitems, Func<T, string> getStringForObject, string filterText)
        {
            var filterTextToLower = filterText?.ToLowerInvariant();
            List<T> filteredOrdered = allitems
                .Where(item => getStringForObject(item)?.ToLowerInvariant().Contains(filterTextToLower) == true)
                // first show exact matches
                .OrderBy(item => getStringForObject(item)?.ToLowerInvariant() != filterTextToLower)
                // then items that start with what was typed
                .ThenBy(item => getStringForObject(item)?.ToLowerInvariant().StartsWith(filterTextToLower) != true)
                .ToList();
            return filteredOrdered;
            // 
        }


        #endregion

        #region Selected Item

        public SelectedItemWrapper SelectedItem
        {
            get => Get<SelectedItemWrapper>();
            set
            {
                if (Set(value))
                {
                    SetDefaultObjectName();
                    RefreshDefaultIsCallActivityChecked();
                }
            }
        }

        public AssetTypeInfo SelectedAti
        {
            get => SelectedItem?.BackingObject as AssetTypeInfo;
            set => SelectedItem = AllSelectedItemWrappers.FirstOrDefault(item => item.BackingObject == value) ??
                MakeWrapper(value);
        }

        public EntitySave SelectedEntitySave
        {
            get => SelectedItem?.BackingObject as EntitySave;
            set => SelectedItem = AllSelectedItemWrappers.FirstOrDefault(item => item.BackingObject == value) ??
                MakeWrapper(value);
        }

        [DependsOn(nameof(SelectedItem))]
        public string SourceClassType
        {
            get => SelectedItem?.ToString();
            set
            {
                SelectedItem = AllSelectedItemWrappers.FirstOrDefault(item => item.BackingObject == value) ??
                    MakeWrapper(value);
            }
        }

        public ReferencedFileSave SourceFile
        {
            get => SelectedItem?.BackingObject as ReferencedFileSave;
            set => SelectedItem = AllSelectedItemWrappers.FirstOrDefault(item => item.BackingObject == value) ??
                MakeWrapper(value);
        }

        public bool IsTypePredetermined
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsObjectTypeRadioButtonPredetermined
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsTypePredetermined))]
        public bool IsSelectionEnabled => !IsTypePredetermined;


        [DependsOn(nameof(IsSelectionEnabled))]
        [DependsOn(nameof(IsObjectTypeRadioButtonPredetermined))]
        public bool IsObjectTypeGroupBoxEnabled => IsSelectionEnabled &&
            !IsObjectTypeRadioButtonPredetermined;

        #endregion

        #region Object Name

        private void SetDefaultObjectName()
        {
            string nameToAssign = null;

            if (SelectedItem != null)
            {
                if (SourceType == SourceType.File)
                {
                    if(!string.IsNullOrWhiteSpace(SourceNameInFile))
                    {
                        nameToAssign = GetDefaultObjectInFileName();
                    }
                }
                else if( IsSourceClassTypeList)
                {
                    var genericType = SourceClassGenericType;

                    if(string.IsNullOrEmpty(genericType))
                    {
                        nameToAssign = "PositionedObjectList";
                    }
                    else
                    {
                        var strippedGeneric = genericType;
                        if(genericType.Contains("\\"))
                        {
                            strippedGeneric = genericType.Substring(genericType.LastIndexOf("\\") + 1);
                        }
                        if(strippedGeneric.Contains("."))
                        {
                            strippedGeneric = genericType.Substring(genericType.LastIndexOf(".") + 1);
                        }
                        nameToAssign = strippedGeneric + "List";
                    }
                }
                else
                {

                    var classType = SourceClassType;

                    if(classType?.Contains("(") == true)
                    {
                        var first = classType.IndexOf("(");
                        var last = classType.IndexOf(")");

                        if(first > -1 && last > -1 && first < last)
                        {
                            classType = classType.Substring(0, first);
                        }
                    }

                    if (classType?.Contains(".") == true)
                    {
                        // un-qualify if it's something like "FlatRedBall.Sprite"
                        var lastIndex = classType.LastIndexOf(".");
                        classType = classType.Substring(lastIndex + 1);
                    }
                    nameToAssign = classType + "Instance";
                    if (nameToAssign.Contains("/") || nameToAssign.Contains("\\"))
                    {
                        nameToAssign = FileManager.RemovePath(nameToAssign);
                    }

                    nameToAssign = nameToAssign.Replace("<T>", "");
                    nameToAssign = nameToAssign.Replace(" ", "");
                }
            }

            if (!string.IsNullOrEmpty(nameToAssign) && 
                // We should tolerate this being null because the VM can have logic that assigns the type before we have selected anything in Glue.
                // of course, that means that the name must be manually set later since it won't automatically be set based on the type.
                EffectiveElement != null)
            {
                // We need to make sure this is a unique name.
                nameToAssign = StringFunctions.MakeStringUnique(nameToAssign, EffectiveElement.AllNamedObjects);

                ObjectName = nameToAssign;
            }
        }

        public string ObjectName
        {
            get => Get<string>();
            set => Set(value);
        }

        #endregion

        public bool IsCallActivityChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string ActivityRecommendationText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool EffectiveCallActivity => IsGenericType == false || IsCallActivityChecked;

        [DependsOn(nameof(IsGenericType))]
        public Visibility CallActivityCheckBoxVisibility => IsGenericType.ToVisibility();

        // Properties to copy over to the NamedObjectSave when it is created.
        public List<PropertySave> Properties
        {
            get;
            set;
        } = new List<PropertySave>();

        /// <summary>
        /// The element to add to. If not set, the current element is used. This must be set first, as
        /// otherh properties (like setting the name or the SourceClassType) may adjust the name of the element.
        /// </summary>
        public GlueElement ForcedElementToAddTo
        {
            get => Get<GlueElement>();
            set => Set(value);
        }

        public GlueElement EffectiveElement => ForcedElementToAddTo ?? GlueState.Self.CurrentElement;

        public string SourceNameInFile
        {
            get => Get<string>();
            set
            {
                if (Set(value))
                {
                    SetDefaultObjectName();
                }
            }
        }
        public string SourceClassGenericType 
        {
            get => Get<string>();
            set
            {
                if(Set(value))
                {
                    RefreshDefaultIsCallActivityChecked();
                    SetDefaultObjectName();
                }
            }
        }

        public List<string> AvailableListTypes { get; private set; } =
            new List<string>();

        [DependsOn(nameof(SelectedItem))]
        public List<string> AvailableFileSourceNames
        {
            get 
            {
                if((SelectedItem as SelectedItemWrapper)?.BackingObject is ReferencedFileSave selectedRfs)
                {
                    List<string> availableObjects = new List<string>();

                    AvailableNameablesStringConverter.FillListWithAvailableObjects(SourceClassType, availableObjects);

                    return availableObjects;
                }
                return new List<string>();
            }
        }

        public List<AssetTypeInfo> FlatRedBallAndCustomTypes
        { 
            get => Get<List<AssetTypeInfo>>();
            set => Set(value);
        }
        public List<AssetTypeInfo> GumTypes
        {
            get => Get<List<AssetTypeInfo>>();
            set => Set(value);
        }
        public List<EntitySave> AvailableEntities 
        { 
            get => Get<List<EntitySave>>();
            set
            {
                Set(value);
            }
        }
        
        public List<ReferencedFileSave> AvailableFiles 
        {
            get => Get<List<ReferencedFileSave>>();
            set
            {
                Set(value);
            }
        }



        [DependsOn(nameof(IsGenericType))]
        public Visibility ListTypeVisibility
        {
            get
            {
                if(IsGenericType)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        [DependsOn(nameof(SourceType))]
        public Visibility SourceNameVisibility
        {
            get
            {
                if(SourceType == SourceType.File)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        [DependsOn(nameof(SourceType))]
        [DependsOn(nameof(SourceClassType))]
        private bool IsGenericType
        {
            get
            {
                return IsSourceClassTypeList; // eventually there could be other types of generics, so let's keep this property here
            }
        }

        private bool IsSourceClassTypeList =>
            this.SourceType == SaveClasses.SourceType.FlatRedBallType && (SourceClassType == "PositionedObjectList (Generic)" || SourceClassType == "PositionedObjectList<T>" || SourceClassType == "FlatRedBall.Math.PositionedObjectList<T>");


        public class ActivityRecommendation
        {
            public bool ShouldCallActivity;
            public string Reason;
        }

        /// <summary>
        /// Returns whether the new object being added is a list of entity type which is a derived entity, and if the
        /// target element already has a list of the base type added, and if that base type has activity already called
        /// </summary>
        private ActivityRecommendation GetActivityRecommendation()
        {
            var toReturn = new ActivityRecommendation();
            toReturn.ShouldCallActivity = true;
            toReturn.Reason = Texts.AddObject_ActivityListRecommended;
            var isList = IsSourceClassTypeList;
            var listEntityType = ObjectFinder.Self.GetEntitySave(SourceClassGenericType);
            var isDerived = listEntityType?.BaseEntity != null;

            if(!isList || listEntityType == null)
            {
                toReturn.ShouldCallActivity = true;
            }
            else
            {
                var baseElements = ObjectFinder.Self.GetAllBaseElementsRecursively(listEntityType);

                var baseElementNames = baseElements.Select(item => item.Name).ToHashSet(); 

                // Don't use "AllNamedObjects" because we only care about top-level objects
                //var baseListTypes = EffectiveElement.AllNamedObjects
                var baseListTypes = EffectiveElement.NamedObjects
                    .Where(item => item.IsList && baseElementNames.Contains(item.SourceClassGenericType) && item.CallActivity)
                    .ToList();

                var thisType = EffectiveElement.NamedObjects
                    .FirstOrDefault(item => item.IsList && item.SourceClassGenericType == this.SourceClassGenericType && item.CallActivity);

                if (baseListTypes.Count > 0)
                {
                    toReturn.ShouldCallActivity = false;

                    var firstBaseType = baseListTypes.First(item => item.CallActivity);

                    toReturn.Reason = String.Format(Texts.AddObject_ActivityCalledByBaseList, firstBaseType.FieldName);
                }
                else if(thisType != null)
                {
                    toReturn.ShouldCallActivity = false;
                    toReturn.Reason = String.Format(Texts.AddObject_ActivityCalledBySameList, thisType.FieldName);
                }

            }


            return toReturn;
        }

        void RefreshDefaultIsCallActivityChecked()
        {
            if(CallActivityCheckBoxVisibility == Visibility.Visible)
            {
                var recommendation = GetActivityRecommendation();
                IsCallActivityChecked = recommendation.ShouldCallActivity;
                ActivityRecommendationText = recommendation.Reason;
            }
        }

        List<SelectedItemWrapper> AllSelectedItemWrappers { get; set; } = new List<SelectedItemWrapper>();

        public event Action StrongSelect;

        SelectedItemWrapper MakeWrapper(object backingObject)
        {
            var toReturn = new SelectedItemWrapper();

            toReturn.BackingObject = backingObject;
            toReturn.StrongSelect += () => StrongSelect?.Invoke() ;

            return toReturn;
        }

        public void RefreshAllSelectedItems()
        {
            AllSelectedItemWrappers.Clear();
            switch (SourceType)
            {
                case SourceType.FlatRedBallType:
                    AllSelectedItemWrappers.AddRange(FlatRedBallAndCustomTypes.Select(item => MakeWrapper(item)));
                    break;
                case SourceType.Gum:
                    AllSelectedItemWrappers.AddRange(GumTypes.Select(item => MakeWrapper(item)));
                    break;
                case SourceType.Entity:
                    AllSelectedItemWrappers.AddRange(AvailableEntities.Select(item => MakeWrapper(item )));
                    break;
                case SourceType.File:
                    AllSelectedItemWrappers.AddRange(AvailableFiles.Select(item => MakeWrapper(item )));
                    break;
            }
        }

        void SelectIfNoSelection()
        {
            if(SelectedItem == null && FilteredItems.Count > 0)
            {
                SelectedItem = FilteredItems[0];
            }
        }

        ObservableCollection<SelectedItemWrapper> filteredItems = new ObservableCollection<SelectedItemWrapper>();
        [DependsOn(nameof(FilterText))]
        [DependsOn(nameof(SourceType))]
        public ObservableCollection<SelectedItemWrapper> FilteredItems
        {
            get => filteredItems;
        }

        public void RefreshFilteredItems()
        {
            filteredItems.Clear();
            if (string.IsNullOrWhiteSpace(FilterText))
            {
                foreach (var item in AllSelectedItemWrappers)
                {
                    filteredItems.Add(item);
                }
            }
            else
            {
                var toAdd = Filter(AllSelectedItemWrappers, (item) => item.ToString(), FilterText);

                foreach (var item in toAdd)
                {
                    filteredItems.Add(item);
                }
            }
        }

        [DependsOn(nameof(SelectedItem))]
        public bool IsOkButtonEnabled
        {
            get
            {
                return SelectedItem != null;
            }
        }

        public AddObjectViewModel()
        {
            IsCallActivityChecked = true;
            FlatRedBallAndCustomTypes = new List<AssetTypeInfo>();
            GumTypes = new List<AssetTypeInfo>();
            AvailableEntities = new List<EntitySave>();
            AvailableFiles = new List<ReferencedFileSave>();
        }

        private string GetDefaultObjectInFileName()
        {
            string newName;
            var spaceParen = SourceNameInFile.IndexOf(" (");

            if (spaceParen != -1)
            {
                newName = SourceNameInFile.Substring(0, spaceParen);
            }
            else
            {
                newName = SourceNameInFile;
            }

            // If the user selected "Entire File" we want to make sure the space doesn't show up:
            newName = newName.Replace(" ", "");

            string throwaway;
            bool isInvalid = NameVerifier.IsNamedObjectNameValid(newName, out throwaway);

            if (!isInvalid)
            {
                // let's get the type:
                var split = SourceNameInFile.Split('(', ')');

                var last = split.LastOrDefault(item => !string.IsNullOrEmpty(item));

                if (last != null)
                {
                    var lastDot = last.LastIndexOf('.');

                    newName = last.Substring(lastDot + 1, last.Length - (lastDot + 1));
                }
            }

            return newName;
        }

    }
}
