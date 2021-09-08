using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.PathPlugin.ViewModels
{
    enum SegmentType
    {
        Line,
        Arc
    }

    class PathSegmentViewModel : ViewModel
    {
        public SegmentType SegmentType
        {
            get => Get<SegmentType>();
            set => Set(value);
        }

        [DependsOn(nameof(SegmentType))]
        public bool IsAngleVisible => SegmentType == SegmentType.Arc;

        public float X
        {
            get => Get<float>();
            set => Set(value);
        }

        public float Y
        {
            get => Get<float>();
            set => Set(value);
        }

        public float Angle
        {
            get => Get<float>();
            set => Set(value);
        }


    }
}
