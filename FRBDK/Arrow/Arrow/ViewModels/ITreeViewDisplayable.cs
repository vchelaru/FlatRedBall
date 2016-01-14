using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Arrow.ViewModels
{
    public interface ITreeViewDisplayable
    {
        string DisplayText { get; }
        bool IsSelected { get; set; }
    }
}
