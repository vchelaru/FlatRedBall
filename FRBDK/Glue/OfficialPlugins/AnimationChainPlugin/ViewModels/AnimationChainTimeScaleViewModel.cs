using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.MVVM;

namespace OfficialPlugins.AnimationChainPlugin.ViewModels;

internal enum ProportionalOrUniform
{
    Proportional,
    Uniform
}

internal class AnimationChainTimeScaleViewModel : ViewModel
{


    private readonly AnimationChainViewModel _animationChainViewModel;

    public ProportionalOrUniform ProportionalOrUniform
    {
        get => Get<ProportionalOrUniform>();
        set => Set(value);
    }

    [DependsOn(nameof(ProportionalOrUniform))]
    public bool IsProportionalChecked
    {
        get => ProportionalOrUniform == ProportionalOrUniform.Proportional;
        set
        {
            if (value)
            {
                ProportionalOrUniform = ProportionalOrUniform.Proportional;
            }
        }
    }

    [DependsOn(nameof(ProportionalOrUniform))]
    public bool IsUniformChecked
    {
        get => ProportionalOrUniform == ProportionalOrUniform.Uniform;
        set
        {
            if(value)
            {
                ProportionalOrUniform = ProportionalOrUniform.Uniform;
            }
        }
    }

    public double LengthInSeconds 
    {
        get => Get<double>();
        set => Set(value);
    }

    [DependsOn(nameof(LengthInSeconds))]
    public string EachFrameDisplay =>
        $"Set each frame time to " +
        $"{LengthInSeconds / _animationChainViewModel.VisibleChildren.Count} seconds";


    public AnimationChainTimeScaleViewModel(AnimationChainViewModel animationChainViewModel)
    {
        _animationChainViewModel = animationChainViewModel;

        this.LengthInSeconds = animationChainViewModel.VisibleChildren.Sum(
            x => x.LengthInSeconds);
    }

    public void ApplyToAnimation()
    {
        switch(ProportionalOrUniform)
        {
            case ProportionalOrUniform.Uniform:
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
            case ProportionalOrUniform.Proportional:
                {
                    var oldTotal = _animationChainViewModel.VisibleChildren.Sum(
                        frame => frame.LengthInSeconds);

                    foreach(var child in _animationChainViewModel.VisibleChildren)
                    {
                        var ratio = child.LengthInSeconds / oldTotal;

                        child.LengthInSeconds = (float)(ratio * LengthInSeconds);
                    }
                }

                break;
        }
    }
}
