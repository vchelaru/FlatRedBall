using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.Glue.MVVM;
using System;

namespace OfficialPlugins.AnimationChainPlugin.ViewModels
{
    internal abstract class ShapeViewModel : ViewModel
    {
        [DependsOn(nameof(Name))]
        public string Text => Name;

        public string Name
        {
            get
            {
                return internalName;
            }
        }

        public AnimationFrameViewModel Parent { get; protected set; }
        protected string internalName;
    }

    internal class RectangleViewModel : ShapeViewModel
    {
        public AxisAlignedRectangleSave BackingModel { get; set; }

        public float Width { get; private set; }
        public float Height { get; private set; }
        public float X { get; private set; }
        public float Y { get; private set; }

        internal void SetFrom(AnimationFrameViewModel animationFrame, AxisAlignedRectangleSave rect)
        {
            BackingModel = rect;
            Parent = animationFrame;

            internalName = rect.Name;
            Width = rect.Width;
            Height = rect.Height;
            X = rect.X;
            Y = rect.Y;
        }
    }

    internal class CircleViewModel : ShapeViewModel
    {
        public CircleSave BackingModel { get; set; }

        public float Radius { get; private set; }
        public float X { get; private set; }
        public float Y { get; private set; }

        internal void SetFrom(AnimationFrameViewModel animationFrame, CircleSave circ)
        {
            BackingModel = circ;
            Parent = animationFrame;

            internalName = circ.Name;
            Radius = circ.Radius;
            X = circ.X;
            Y = circ.Y;
        }
    }
}
