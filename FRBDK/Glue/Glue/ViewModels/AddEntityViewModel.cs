using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;

namespace GlueFormsCore.ViewModels
{
    public class AddEntityViewModel : ViewModel
    {
        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }


        public bool IsSpriteChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsTextChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsCircleChecked
        {
            get => Get<bool>();
            set
            {
                if(Set(value) && value && !hasExplicitlyUncheckedICollidable)
                {
                    IsICollidableChecked = true;
                }
            }
        }

        public bool IsAxisAlignedRectangleChecked
        {
            get => Get<bool>();
            set
            {
                if (Set(value) && value && !hasExplicitlyUncheckedICollidable)
                {
                    IsICollidableChecked = true;
                }
            }
        }

        public bool IsPolygonChecked
        {
            get => Get<bool>();
            set
            {
                if (Set(value) && value && !hasExplicitlyUncheckedICollidable)
                {
                    IsICollidableChecked = true;
                }
            }
        }

        // Not shown in the UI since this is never really used but we'll keep it here in case that changes in the future
        public bool IsIVisibleChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        // Not shown in the UI since this is never really used but we'll keep it here in case that changes in the future
        public bool IsIClickableChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        // Not shown in the UI since this is never really used but we'll keep it here in case that changes in the future
        public bool IsIWindowChecked
        {
            get => Get<bool>();
            set
            {
                if(Set(value) && value)
                {
                    IsIVisibleChecked = true;
                }
            }
        }

        [DependsOn(nameof(Name))]
        public string FailureText
        {
            get
            {
                var isValid = NameVerifier.IsEntityNameValid(Name, null, out string whyIsntValid);

                if (!isValid)
                {
                    return whyIsntValid;
                }
                else
                {
                    return null;
                }
            }
        }

        [DependsOn(nameof(FailureText))]
        public Visibility FailureTextVisibility => string.IsNullOrWhiteSpace(FailureText) ?
            Visibility.Collapsed : Visibility.Visible;

        public ObservableCollection<string> BaseEntityOptions
        {
            get; set;
        } = new ObservableCollection<string>();

        public string SelectedBaseEntity
        {
            get => Get<string>();
            set => Set(value);
        }

        [DependsOn(nameof(SelectedBaseEntity))]
        public bool HasInheritance =>
            SelectedBaseEntity != "<NONE>" && !string.IsNullOrEmpty(SelectedBaseEntity);

        [DependsOn(nameof(SelectedBaseEntity))]
        public Visibility InterfaceVisibility =>
            (HasInheritance == false).ToVisibility();

        [DependsOn(nameof(SelectedBaseEntity))]
        public Visibility VisualsVisibility =>
            (HasInheritance == false).ToVisibility();

        [DependsOn(nameof(SelectedBaseEntity))]
        public Visibility CollisionsVisibility =>
            (HasInheritance == false).ToVisibility();


        bool hasExplicitlyUncheckedICollidable;

        public bool IsICollidableChecked
        {
            get => Get<bool>();
            set
            {
                if (Set(value) && !value)
                {
                    hasExplicitlyUncheckedICollidable = true;
                }
            }
        }

        public bool IsIDamageableChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsIDamageAreaChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsICollidableEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsCreateFactoryChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public AddEntityViewModel()
        {
            IsICollidableEnabled = true;
            IsCreateFactoryChecked = true;
        }
    }
}
