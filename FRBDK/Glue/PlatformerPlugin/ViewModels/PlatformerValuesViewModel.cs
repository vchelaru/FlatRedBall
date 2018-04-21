using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.PlatformerPlugin.SaveClasses;
using FlatRedBall.Glue.MVVM;
using System.Windows;
using System.ComponentModel;

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

        bool isImmediate = true;
        public bool IsImmediate
        {
            get { return isImmediate; }
            set
            {
                base.ChangeAndNotify(ref isImmediate, value);
                NotifyPropertyChanged(nameof(UsesAcceleration));
                NotifyPropertyChanged(nameof(AccelerationValuesVisibility));
            }
        }

        // Can't use DependsOn because I think it's a circular refernece. Would have to update the VM to handle that...
        public bool UsesAcceleration
        {
            get { return !isImmediate; }
            set
            {
                base.ChangeAndNotify(ref isImmediate, !value);
                NotifyPropertyChanged(nameof(IsImmediate));
                NotifyPropertyChanged(nameof(AccelerationValuesVisibility));
            }
        }

        public Visibility AccelerationValuesVisibility
        {
            get
            {
                if (isImmediate)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }

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
            get { return Get<float>(); }
            set { Set(value); }
        }

        public float DecelerationTimeX
        {
            get { return Get<float>(); }
            set { Set(value); }
        }

        public float Gravity
        {
            get { return Get<float>(); }
            set { Set(value); }
        }

        public float MaxFallSpeed
        {
            get { return Get<float>(); }
            set { Set(value); }
        }

        public float JumpVelocity
        {
            get { return Get<float>(); }
            set { Set(value); }
        }

        public float JumpApplyLength
        {
            get { return Get<float>(); }
            set { Set(value); }
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
            get { return Get<decimal>(); }
            set { Set(value); }
        }

        public decimal DownhillMaxSpeedBoostPercentage
        {
            get { return Get<decimal>(); }
            set { Set(value); }
        }

        [DependsOn(nameof(JumpApplyByButtonHold))]
        public Visibility JumpHoldTimeVisibility
        {
            get
            {
                if (JumpApplyByButtonHold)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        #endregion

        #region Constructor/Clone

        public PlatformerValuesViewModel()
        {
            this.PropertyChanged += HandlePropertyChanged;
        }

        public PlatformerValuesViewModel Clone()
        {
            return (PlatformerValuesViewModel)this.MemberwiseClone();
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

        internal void SetFrom(PlatformerValues values)
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

            MoveSameSpeedOnSlopes = values.MoveSameSpeedOnSlopes;
            UphillFullSpeedSlope = values.UphillFullSpeedSlope;
            UphillStopSpeedSlope = values.UphillStopSpeedSlope;
            DownhillFullSpeedSlope = values.DownhillFullSpeedSlope;
            DownhillMaxSpeedSlope = values.DownhillMaxSpeedSlope;
            DownhillMaxSpeedBoostPercentage = values.DownhillMaxSpeedBoostPercentage;
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










            return toReturn;
        }
    }
}
