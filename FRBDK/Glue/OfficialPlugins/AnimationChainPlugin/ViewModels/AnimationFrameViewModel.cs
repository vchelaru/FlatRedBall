using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace OfficialPlugins.AnimationChainPlugin.ViewModels
{
    internal class AnimationFrameViewModel : ViewModel
    {
        public AnimationFrameViewModel()
        {
            VisibleChildren = new ObservableCollection<ShapeViewModel>();
        }

        public AnimationFrameSave BackingModel { get; set; }
        public AnimationChainViewModel Parent { get; private set; }
        public float LengthInSeconds
        {
            get => Get<float>();
            set => Set(value);
        }

        public string StrippedTextureName
        {
            get => Get<string>();
            set => Set(value);
        }

        public float LeftCoordinate
        {
            get => Get<float>();
            set => Set(value);
        }


        public float TopCoordinate
        {
            get => Get<float>();
            set => Set(value);
        }


        public float RightCoordinate
        {
            get => Get<float>();
            set => Set(value);
        }


        public float BottomCoordinate
        {
            get => Get<float>();
            set => Set(value);
        }

        public bool FlipHorizontal
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool FlipVertical
        {
            get => Get<bool>();
            set => Set(value);
        }

        public ObservableCollection<ShapeViewModel> VisibleChildren
        {
            get => Get<ObservableCollection<ShapeViewModel>>();
            set => Set(value);
        }

        int ResolutionWidth;
        int ResolutionHeight;

        [DependsOn(nameof(LengthInSeconds))]
        public string Text => $"{LengthInSeconds.ToString("0.00")} ({StrippedTextureName})";

        public void SetFrom(AnimationChainViewModel parent, AnimationFrameSave animationFrame, int resolutionWidth, int resolutionHeight)
        {
            BackingModel = animationFrame;
            Parent = parent;
            LengthInSeconds = animationFrame.FrameLength;

            if (!string.IsNullOrEmpty(animationFrame.TextureName))
            {
                StrippedTextureName = FileManager.RemovePath(FileManager.RemoveExtension(animationFrame.TextureName));
            }
            else
            {
                StrippedTextureName = string.Empty;
            }

            LeftCoordinate = animationFrame.LeftCoordinate;
            TopCoordinate = animationFrame.TopCoordinate;
            RightCoordinate = animationFrame.RightCoordinate;
            BottomCoordinate = animationFrame.BottomCoordinate;

            ResolutionWidth = resolutionWidth;
            ResolutionHeight = resolutionHeight;

            FlipHorizontal = animationFrame.FlipHorizontal;
            FlipVertical = animationFrame.FlipVertical;


            var rectangles = animationFrame.ShapeCollectionSave?.AxisAlignedRectangleSaves;
            if (rectangles != null)
                foreach (var rect in rectangles)
                {
                    var shape = new RectangleViewModel();
                    shape.SetFrom(this, rect);
                    VisibleChildren.Add(shape);
                }

            var circles = animationFrame.ShapeCollectionSave?.CircleSaves;
            if (circles != null)
                foreach (var circ in animationFrame.ShapeCollectionSave?.CircleSaves)
                {
                    var shape = new CircleViewModel();
                    shape.SetFrom(this, circ);
                    VisibleChildren.Add(shape);
                }
        }
    }
}
