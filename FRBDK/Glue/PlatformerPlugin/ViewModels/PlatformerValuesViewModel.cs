using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.PlatformerPlugin.SaveClasses;
using FlatRedBall.Glue.MVVM;
using System.Windows;

namespace FlatRedBall.PlatformerPlugin.ViewModels
{
    public class PlatformerValuesViewModel : ViewModel
    {

        string name;
        public string Name
        {
            get { return name; }
            set { base.ChangeAndNotify(ref name, value); }
        }

        float maxSpeedX;
        public float MaxSpeedX
        {
            get { return maxSpeedX; }
            set { base.ChangeAndNotify(ref maxSpeedX, value); }
        }

        bool canFallThroughCloudPlatforms;
        public bool CanFallThroughCloudPlatforms
        {
            get { return canFallThroughCloudPlatforms; }
            set { base.ChangeAndNotify(ref canFallThroughCloudPlatforms, value); }
        }

        [DependsOn(nameof(CanFallThroughCloudPlatforms))]
        public Visibility FallThroughCloudPlatformsVisibility =>
            CanFallThroughCloudPlatforms ? Visibility.Visible : Visibility.Hidden;

        float cloudFallThroughDistance;
        public float CloudFallThroughDistance
        {
            get { return cloudFallThroughDistance; }
            set { base.ChangeAndNotify(ref cloudFallThroughDistance, value); }
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
                if(isImmediate)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }
        float accelerationTimeX;
        public float AccelerationTimeX
        {
            get { return accelerationTimeX; }
            set { base.ChangeAndNotify(ref accelerationTimeX, value); }
        }

        float decelerationTimeX;
        public float DecelerationTimeX
        {
            get { return decelerationTimeX; }
            set { base.ChangeAndNotify(ref decelerationTimeX, value); }
        }

        float gravity;
        public float Gravity
        {
            get { return gravity; }
            set { base.ChangeAndNotify(ref gravity, value); }
        }

        float maxFallSpeed;
        public float MaxFallSpeed
        {
            get { return maxFallSpeed; }
            set { base.ChangeAndNotify(ref maxFallSpeed, value); }
        }

        float jumpVelocity;
        public float JumpVelocity
        {
            get { return jumpVelocity; }
            set { base.ChangeAndNotify(ref jumpVelocity, value); }
        }

        float jumpApplyLength;
        public float JumpApplyLength
        {
            get { return jumpApplyLength; }
            set { base.ChangeAndNotify(ref jumpApplyLength, value); }
        }

        bool jumpApplyByButtonHold;
        public bool JumpApplyByButtonHold
        {
            get { return jumpApplyByButtonHold; }
            set
            {
                base.ChangeAndNotify(ref jumpApplyByButtonHold, value);
            }
        }

        [DependsOn(nameof(JumpApplyByButtonHold))]
        public Visibility JumpHoldTimeVisibility
        {
            get
            {
                if(JumpApplyByButtonHold)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
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
        }

        public PlatformerValuesViewModel Clone()
        {
            return (PlatformerValuesViewModel)this.MemberwiseClone();
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
            return toReturn;
        }
    }
}
