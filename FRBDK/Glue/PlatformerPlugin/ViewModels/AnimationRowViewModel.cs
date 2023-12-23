using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.MVVM;
using PlatformerPluginCore.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace PlatformerPluginCore.ViewModels
{


    public class AnimationRowViewModel : ViewModel
    {
        public string AnimationName
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool HasLeftAndRight 
        {
            get => Get<bool>();
            set => Set(value);
        }

        public float? MinXVelocityAbsolute
        {
            get => Get<float?>();
            set => Set(value);
        }
        public float? MaxXVelocityAbsolute
        {
            get => Get<float?>();
            set => Set(value);
        }

        public float? MinYVelocity
        {
            get => Get<float?>();
            set => Set(value);
        }
        public float? MaxYVelocity
        {
            get => Get<float?>();
            set => Set(value);
        }

        public float? MinHorizontalInputAbsolute
        {
            get => Get<float?>();
            set => Set(value);
        }
        public float? MaxHorizontalInputAbsolute
        {
            get => Get<float?>();
            set => Set(value);
        }

        public float? AbsoluteXVelocityAnimationSpeedMultiplier
        {
            get => Get<float?>();
            set => Set(value);
        }
        public float? AbsoluteYVelocityAnimationSpeedMultiplier
        {
            get => Get<float?>();
            set => Set(value);
        }

        public float? MaxSpeedXRatioMultiplier
        {
            get => Get<float?>();
            set => Set(value);
        }
        public float? MaxSpeedYRatioMultiplier
        {
            get => Get<float?>();
            set => Set(value);
        }

        public bool? OnGroundRequirement
        {
            get => Get<bool?>();
            set => Set(value);
        }

        public string MovementName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string CustomCondition
        {
            get => Get<string>();
            set => Set(value);
        }

        public AnimationSpeedAssignment AnimationSpeedAssignment
        {
            get => Get<AnimationSpeedAssignment>();
            set => Set(value);
        }

        // If adding any properties here, update ApplyTo and SetFrom
        public ICommand MoveUpCommand
        {
            get;
            set;
        }

        public ICommand MoveDownCommand
        {
            get;
            set;
        }

        public ICommand RemoveCommand
        {
            get;
            set;
        }

        public ICommand DuplicateCommand
        {
            get; set;
        }

        public event Action MoveUp;
        public event Action MoveDown;
        public event Action Remove;
        public event Action Duplicate;

        public string Notes
        {
            get => Get<string>();
            set => Set(value);
        }

        public AnimationRowViewModel()
        {
            HasLeftAndRight = true;

            MoveUpCommand = new Command(() => MoveUp?.Invoke());
            MoveDownCommand = new Command(() => MoveDown?.Invoke());
            RemoveCommand = new Command(() => Remove?.Invoke());
            DuplicateCommand = new Command(() => Duplicate?.Invoke());
        }

        public void ApplyTo(IndividualPlatformerAnimationValues model)
        {
            model.AnimationName = AnimationName;
            model.HasLeftAndRight = HasLeftAndRight;

            model.MinXVelocityAbsolute = MinXVelocityAbsolute;
            model.MaxXVelocityAbsolute = MaxXVelocityAbsolute;

            model.MinYVelocity = MinYVelocity;
            model.MaxYVelocity = MaxYVelocity;

            model.MinHorizontalInputAbsolute = MinHorizontalInputAbsolute;
            model.MaxHorizontalInputAbsolute = MaxHorizontalInputAbsolute;


            model.AbsoluteXVelocityAnimationSpeedMultiplier = AbsoluteXVelocityAnimationSpeedMultiplier;
            model.AbsoluteYVelocityAnimationSpeedMultiplier = AbsoluteYVelocityAnimationSpeedMultiplier;

            model.MaxSpeedXRatioMultiplier = MaxSpeedXRatioMultiplier;
            model.MaxSpeedYRatioMultiplier = MaxSpeedYRatioMultiplier;


            model.OnGroundRequirement = OnGroundRequirement;

            model.AnimationSpeedAssignment = AnimationSpeedAssignment;

            model.Notes = Notes;

            model.MovementName = MovementName;
            model.CustomCondition = CustomCondition;
        }

        public void SetFrom(IndividualPlatformerAnimationValues model)
        {
            this.AnimationName = model.AnimationName;
            this.HasLeftAndRight = model.HasLeftAndRight;

            this.MinXVelocityAbsolute = model.MinXVelocityAbsolute;
            this.MaxXVelocityAbsolute = model.MaxXVelocityAbsolute;

            this.MinYVelocity = model.MinYVelocity;
            this.MaxYVelocity = model.MaxYVelocity;

            this.MinHorizontalInputAbsolute = model.MinHorizontalInputAbsolute;
            this.MaxHorizontalInputAbsolute = model.MaxHorizontalInputAbsolute;

            this.AbsoluteXVelocityAnimationSpeedMultiplier = model.AbsoluteXVelocityAnimationSpeedMultiplier;
            this.AbsoluteYVelocityAnimationSpeedMultiplier = model.AbsoluteYVelocityAnimationSpeedMultiplier;

            this.OnGroundRequirement = model.OnGroundRequirement;

            this.AnimationSpeedAssignment = model.AnimationSpeedAssignment;

            this.MaxSpeedXRatioMultiplier = model.MaxSpeedXRatioMultiplier;
            this.MaxSpeedYRatioMultiplier = model.MaxSpeedYRatioMultiplier;

            this.MovementName = model.MovementName;
            this.CustomCondition = model.CustomCondition;

            this.Notes = model.Notes;
        }
    }
}
