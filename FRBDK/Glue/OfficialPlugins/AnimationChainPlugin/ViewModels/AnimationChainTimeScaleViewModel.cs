using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.MVVM;

namespace OfficialPlugins.AnimationChainPlugin.ViewModels;

internal enum TimeAssignmentType
{
    Proportional,
    Uniform,
    SetFrameTimeDirectly
}

internal class AnimationChainTimeScaleViewModel : ViewModel
{


    private readonly AnimationChainViewModel _animationChainViewModel;

    public TimeAssignmentType TimeAssignmentType
    {
        get => Get<TimeAssignmentType>();
        set => Set(value);
    }

    [DependsOn(nameof(TimeAssignmentType))]
    public bool IsProportionalChecked
    {
        get => TimeAssignmentType == TimeAssignmentType.Proportional;
        set
        {
            if (value)
            {
                TimeAssignmentType = TimeAssignmentType.Proportional;
            }
        }
    }

    [DependsOn(nameof(TimeAssignmentType))]
    public bool IsUniformChecked
    {
        get => TimeAssignmentType == TimeAssignmentType.Uniform;
        set
        {
            if(value)
            {
                TimeAssignmentType = TimeAssignmentType.Uniform;
            }
        }
    }

    [DependsOn(nameof(TimeAssignmentType))]
    public bool IsSetFrameTimeDirectlyChecked
    {
        get => TimeAssignmentType == TimeAssignmentType.SetFrameTimeDirectly;
        set
        {
            if(value)
            {
                TimeAssignmentType = TimeAssignmentType.SetFrameTimeDirectly;
            }
        }
    }

    public decimal LengthInSeconds 
    {
        get => Get<decimal>();
        set => Set(value);
    }

    [DependsOn(nameof(LengthInSeconds))]
    public string EachFrameDisplay =>
        $"Divide timeanimation time evently - set each frame time to " +
        $"{LengthInSeconds / _animationChainViewModel.VisibleChildren.Count} seconds";

    [DependsOn(nameof(TimeAssignmentType))]
    public string DesiredAnimationOrFrameDisplay =>
        TimeAssignmentType == TimeAssignmentType.SetFrameTimeDirectly ?
        "Each Frame Time (seconds):" :
        "Desired Animation Time (seconds):";

    public AnimationChainTimeScaleViewModel(AnimationChainViewModel animationChainViewModel)
    {
        _animationChainViewModel = animationChainViewModel;

        this.LengthInSeconds = animationChainViewModel.VisibleChildren.Sum(
            // use decimal for better math and precision.
            // using double can result in a value like 0.1
            // being widened and stored as 0.10000000149011612
            x => (decimal)x.LengthInSeconds);

        double asDouble = animationChainViewModel.VisibleChildren[0].LengthInSeconds;
        decimal asDecimal = (decimal)animationChainViewModel.VisibleChildren[0].LengthInSeconds;


    }

    public void ApplyToAnimation()
    {
        switch(TimeAssignmentType)
        {
            case TimeAssignmentType.Uniform:
                {
                    var lengthInSeconds =
                        LengthInSeconds /
                        _animationChainViewModel.VisibleChildren.Count;
                    foreach(var child in _animationChainViewModel.VisibleChildren)
                    {
                        child.LengthInSeconds =  (float) lengthInSeconds;
                    }
                }
                break;
            case TimeAssignmentType.Proportional:
                {
                    var oldTotal = _animationChainViewModel.VisibleChildren.Sum(
                        frame => frame.LengthInSeconds);

                    foreach(var child in _animationChainViewModel.VisibleChildren)
                    {
                        var ratio = child.LengthInSeconds / oldTotal;

                        child.LengthInSeconds = (float)(ratio * (float)LengthInSeconds);
                    }
                }

                break;
            case TimeAssignmentType.SetFrameTimeDirectly:
                {
                    foreach(var child in _animationChainViewModel.VisibleChildren)
                    {
                        child.LengthInSeconds = (float)LengthInSeconds;
                    }
                }
                break;
        }
    }
}
