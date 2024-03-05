using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TopDownPlugin.Models;

namespace TopDownPlugin.ViewModels
{
    public class AnimationRowViewModel : ViewModel
    {
        public string AnimationName
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsDirectionFacingAppended
        {
            get => Get<bool>();
            set => Set(value);
        }

        public float? MinVelocityAbsolute
        {
            get => Get<float?>();
            set => Set(value);
        }

        public float? MaxVelocityAbsolute
        {
            get => Get<float?>();
            set => Set(value);
        }

        public float? AbsoluteVelocityAnimationSpeedMultiplier
        {
            get => Get<float?>();
            set => Set(value);
        }

        public float? MinMovementInputAbsolute
        {
            get => Get<float?>();
            set => Set(value);
        }

        public float? MaxMovementInputAbsolute
        {
            get => Get<float?>();
            set => Set(value);
        }

        public float? MaxSpeedRatioMultiplier
        {
            get => Get<float?>();
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
            IsDirectionFacingAppended = true;

            MoveUpCommand = new Command(() => MoveUp?.Invoke());
            MoveDownCommand = new Command(() => MoveDown?.Invoke());
            RemoveCommand = new Command(() => Remove?.Invoke());
            DuplicateCommand = new Command(() => Duplicate?.Invoke());
        }

        public void ApplyTo(IndividualTopDownAnimationValues model)
        {
            model.AnimationName = AnimationName;
            model.IsDirectionFacingAppended = IsDirectionFacingAppended;
            model.MinVelocityAbsolute = MinVelocityAbsolute;
            model.MaxVelocityAbsolute = MaxVelocityAbsolute;
            model.AbsoluteVelocityAnimationSpeedMultiplier = AbsoluteVelocityAnimationSpeedMultiplier;
            model.MinMovementInputAbsolute = MinMovementInputAbsolute;
            model.MaxMovementInputAbsolute = MaxMovementInputAbsolute;
            model.MaxSpeedRatioMultiplier = MaxSpeedRatioMultiplier;
            model.MovementName = MovementName;
            model.CustomCondition = CustomCondition;
            model.AnimationSpeedAssignment = AnimationSpeedAssignment;
            model.Notes = Notes;
        }

        public void SetFrom(IndividualTopDownAnimationValues model)
        {
            AnimationName = model.AnimationName;
            IsDirectionFacingAppended = model.IsDirectionFacingAppended;
            MinVelocityAbsolute = model.MinVelocityAbsolute;
            MaxVelocityAbsolute = model.MaxVelocityAbsolute;
            AbsoluteVelocityAnimationSpeedMultiplier = model.AbsoluteVelocityAnimationSpeedMultiplier;
            MinMovementInputAbsolute = model.MinMovementInputAbsolute;
            MaxMovementInputAbsolute = model.MaxMovementInputAbsolute;
            MaxSpeedRatioMultiplier = model.MaxSpeedRatioMultiplier;
            MovementName = model.MovementName;
            CustomCondition = model.CustomCondition;
            AnimationSpeedAssignment = model.AnimationSpeedAssignment;
            Notes = model.Notes;
        }

    }
}
