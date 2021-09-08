using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;

namespace OfficialPlugins.PathPlugin.ViewModels
{
    public class PathViewModel : ViewModel
    {
        public bool UpdateModelOnChanges { get; set; } = true;

        public ObservableCollection<PathSegmentViewModel> PathSegments
        {
            get; set;
        } = new ObservableCollection<PathSegmentViewModel>();

    }
}
