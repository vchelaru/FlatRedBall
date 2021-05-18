using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.PlatformerPlugin.SaveClasses;
using FlatRedBall.Glue.MVVM;
using System.Windows;
using System.ComponentModel;
using GlueCommon.Models;

namespace FlatRedBall.PlatformerPlugin.ViewModels
{
    public class PlatformerValuesViewModel : ViewModel
    {
        #region Fields/Properties

        public string Name
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public float MaxSpeedX
        {
            get { return Get<float>(); }
            set { Set(value); }
        }

        public bool CanFallThroughCloudPlatforms
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        [DependsOn(nameof(CanFallThroughCloudPlatforms))]
        public Visibility FallThroughCloudPlatformsVisibility =>
            CanFallThroughCloudPlatforms ? Visibility.Visible : Visibility.Hidden;

        public float CloudFallThroughDistance
        {
            get { return Get<float>(); }
            set { Set(value); }
        }

        public bool IsImmediate
        {
            get => Get<bool>();
            set
            {
                if (Set(value))
                {
                    NotifyPropertyChanged(nameof(UsesAcceleration));
                }
            }
        }

        // Can't use DependsOn because I think it's a circular refernece. Would have to update the VM to handle that...
        public bool UsesAcceleration
        {
            get { return !IsImmediate; }
            set
            {
                Set(!value, nameof(IsImmediate));
            }
        }

        [DependsOn(nameof(IsImmediate))]
        public Visibility AccelerationValuesVisibility => (!IsImmediate).ToVisibility();

        bool moveSameSpeedOnSlopes;
        public bool MoveSameSpeedOnSlopes
        {
            get { return moveSameSpeedOnSlopes; }
            set
            {
                base.ChangeAndNotify(ref moveSameSpeedOnSlopes, value);
                NotifyPropertyChanged(nameof(AdjustSpeedOnSlopes));
            }
        }

        public bool AdjustSpeedOnSlopes
        {
            get { return !moveSameSpeedOnSlopes; }
            set
            {
                base.ChangeAndNotify(ref moveSameSpeedOnSlopes, !value);
                NotifyPropertyChanged(nameof(MoveSameSpeedOnSlopes));

            }
        }

        [DependsOn(nameof(MoveSameSpeedOnSlopes))]
        public Visibility SlopeMovementSpeedUiVisibility
        {
            get
            {
                if (MoveSameSpeedOnSlopes)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }


        public float AccelerationTimeX
        {
            get => Get<float>(); 
            set => Set(value); 
        }

        public float DecelerationTimeX
        {
            get => Get<float>();
            set => Set(value);
        }

        public bool IsCustomDecelerationChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsCustomDecelerationChecked))]
        public bool IsCustomDecelerationValueEnabled => IsCustomDecelerationChecked;

        public float CustomDecelerationValue
        {
            get => Get<float>();
            set => Set(value);
        }

        public float Gravity
        {
            get => Get<float>();
            set => Set(value);
        }

        [DependsOn(nameof(Gravity))]
        public Visibility Gravity0WarningVisibility => (Gravity <= 0).ToVisibility();


        public float MaxFallSpeed
        {
            get => Get<float>();
            set => Set(value);
        }

        [DependsOn(nameof(MaxFallSpeed))]
        public Visibility MaxFallingSpeed0WarningVisibility => (MaxFallSpeed <= 0).ToVisibility();

        public float JumpVelocity
        {
            get => Get<float>();
            set => Set(value);
        }

        public float JumpApplyLength
        {
            get => Get<float>();
            set => Set(value);
        }

        public bool JumpApplyByButtonHold
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        public decimal UphillFullSpeedSlope
        {
            get { return Get<decimal>(); }
            set { Set(value); }
        }

        public decimal UphillStopSpeedSlope
        {
            get { return Get<decimal>(); }
            set { Set(value); }
        }

        public decimal DownhillFullSpeedSlope
        {
            get { return Get<decimal>(); }
            set { Set(value); }
        }

        public decimal DownhillMaxSpeedSlope
        {
            get => Get<decimal>(); 
            set => Set(value); 
        }

        public decimal DownhillMaxSpeedBoostPercentage
        {
            get => Get<decimal>(); 
            set => Set(value); 
        }

        [DependsOn(nameof(JumpApplyByButtonHold))]
        public Visibility JumpHoldTimeVisibility => JumpApplyByButtonHold.ToVisibility();

        public bool CanClimb
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(CanClimb))]
        public Visibility ClimbingUiVisibility => CanClimb.ToVisibility();

        public float MaxClimbingSpeed
        {
            get => Get<float>();
            set => Set(value);
        }

        public InheritOrOverwrite InheritOrOverwrite
        {
            get => Get<InheritOrOverwrite>();
            set => Set(value);
        }

        [DependsOn(nameof(InheritOrOverwrite))]
        public bool InheritMovementValues
        {
            get => InheritOrOverwrite == InheritOrOverwrite.Inherit;
            set
            {
                Set(value);
                if (value)
                {
                    InheritOrOverwrite = InheritOrOverwrite.Inherit;
                }
            }
        }

        [DependsOn(nameof(InheritOrOverwrite))]
        public bool OverwriteMovementValues
        {
            get => InheritOrOverwrite == InheritOrOverwrite.Overwrite;
            set
            {
                Set(value);
                if (value)
                {
                    InheritOrOverwrite = InheritOrOverwrite.Overwrite;
                }
            }
        }

        public bool IsInDerivedPlatformerEntity
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsInDerivedPlatformerEntity))]
        public Visibility InheritBoxVisibility => IsInDerivedPlatformerEntity.ToVisibility();

        [DependsOn(nameof(IsInDerivedPlatformerEntity))]
        public Visibility DeleteButtonVisibility => (IsInDerivedPlatformerEntity == false).ToVisibility();

        [DependsOn(nameof(InheritOrOverwrite))]
        [DependsOn(nameof(IsInDerivedPlatformerEntity))]
        public bool IsEditable => InheritOrOverwrite == InheritOrOverwrite.Overwrite || IsInDerivedPlatformerEntity == false;

        [DependsOn(nameof(IsInDerivedPlatformerEntity))]
        public bool IsNameEditable => IsInDerivedPlatformerEntity == false;



        #endregion

        #region Constructor/Clone

        public PlatformerValuesViewModel()
        {
            this.PropertyChanged += HandlePropertyChanged;
        }

        public PlatformerValuesViewModel Clone()
        {
            //return (PlatformerValuesViewModel)this.MemberwiseClone();

            string serialized = null;
            FlatRedBall.IO.FileManager.XmlSerialize(this, out serialized);

            return FlatRedBall.IO.FileManager.XmlDeserializeFromString<PlatformerValuesViewModel>(serialized);
        }

        #endregion

        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(UphillFullSpeedSlope):
                case nameof(UphillStopSpeedSlope):
                    ClampUphillValues();
                    break;
                case nameof(DownhillFullSpeedSlope):
                case nameof(DownhillMaxSpeedSlope):
                    ClampDownhillValues();
                    break;
            }
        }

        internal void SetFrom(PlatformerValues values, bool isInDerivedPlatformer)
        {
            Name = values.Name;
            MaxSpeedX = values.MaxSpeedX;
            AccelerationTimeX = values.AccelerationTimeX;
            DecelerationTimeX = values.DecelerationTimeX;
            Gravity = values.Gravity;
            MaxFallSpeed = values.MaxFallSpeed;
            JumpVelocity = values.JumpVelocity;
            JumpApplyLength = values.JumpApplyLength;
            JumpApplyByButtonHold = values.JumpApplyByButtonHold;
            UsesAcceleration = values.UsesAcceleration;
            CanFallThroughCloudPlatforms = values.CanFallThroughCloudPlatforms;
            CloudFallThroughDistance = values.CloudFallThroughDistance;

            this.IsCustomDecelerationChecked = values.IsUsingCustomDeceleration;
            this.CustomDecelerationValue = values.CustomDecelerationValue;

            MoveSameSpeedOnSlopes = values.MoveSameSpeedOnSlopes;
            UphillFullSpeedSlope = values.UphillFullSpeedSlope;
            UphillStopSpeedSlope = values.UphillStopSpeedSlope;
            DownhillFullSpeedSlope = values.DownhillFullSpeedSlope;
            DownhillMaxSpeedSlope = values.DownhillMaxSpeedSlope;
            DownhillMaxSpeedBoostPercentage = values.DownhillMaxSpeedBoostPercentage;

            CanClimb = values.CanClimb;
            MaxClimbingSpeed = values.MaxClimbingSpeed;

            this.InheritOrOverwrite = values.InheritOrOverwrite;
            this.IsInDerivedPlatformerEntity = isInDerivedPlatformer;
        }

        private void ClampUphillValues()
        {
            UphillStopSpeedSlope =
                System.Math.Max(0, UphillStopSpeedSlope);
            UphillStopSpeedSlope =
                System.Math.Min(90, UphillStopSpeedSlope);

            UphillFullSpeedSlope =
                System.Math.Max(0, UphillFullSpeedSlope);
            UphillFullSpeedSlope =
                System.Math.Min(UphillFullSpeedSlope, UphillStopSpeedSlope);
        }

        private void ClampDownhillValues()
        {
            DownhillFullSpeedSlope =
                System.Math.Max(0, DownhillFullSpeedSlope);
            DownhillFullSpeedSlope =
                System.Math.Min(90, DownhillFullSpeedSlope);

            DownhillMaxSpeedSlope =
                System.Math.Max(DownhillFullSpeedSlope, DownhillMaxSpeedSlope);

            DownhillMaxSpeedSlope =
                System.Math.Min(90, DownhillMaxSpeedSlope);

        }

        internal PlatformerValues ToValues()
        {
            var toReturn = new PlatformerValues();


            toReturn.Name = Name;
            toReturn.MaxSpeedX = MaxSpeedX;

            toReturn.AccelerationTimeX = AccelerationTimeX;
            toReturn.DecelerationTimeX = DecelerationTimeX;
            toReturn.Gravity = Gravity;
            toReturn.MaxFallSpeed = MaxFallSpeed;
            toReturn.JumpVelocity = JumpVelocity;
            toReturn.JumpApplyLength = JumpApplyLength;
            toReturn.JumpApplyByButtonHold = JumpApplyByButtonHold;
            toReturn.UsesAcceleration = UsesAcceleration;
            toReturn.CanFallThroughCloudPlatforms = CanFallThroughCloudPlatforms;
            toReturn.CloudFallThroughDistance = CloudFallThroughDistance;

            toReturn.MoveSameSpeedOnSlopes = MoveSameSpeedOnSlopes;
            toReturn.UphillFullSpeedSlope = UphillFullSpeedSlope;
            toReturn.UphillStopSpeedSlope = UphillStopSpeedSlope;
            toReturn.DownhillFullSpeedSlope = DownhillFullSpeedSlope;
            toReturn.DownhillMaxSpeedSlope = DownhillMaxSpeedSlope;
            toReturn.DownhillMaxSpeedBoostPercentage = DownhillMaxSpeedBoostPercentage;

            toReturn.IsUsingCustomDeceleration = this.IsCustomDecelerationChecked;
            toReturn.CustomDecelerationValue = this.CustomDecelerationValue;

            toReturn.CanClimb = this.CanClimb;
            toReturn.MaxClimbingSpeed = this.MaxClimbingSpeed;

            toReturn.InheritOrOverwriteAsInt = (int)this.InheritOrOverwrite;

            return toReturn;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
