using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace FlatRedBall.Glue.ViewModels
{
    internal interface ISearchBarViewModel
    {
        string SearchBoxText { get; set; }

        bool IsSearchBoxFocused { get; set; }

        Visibility SearchButtonVisibility { get;  }

        Visibility TipsVisibility { get; }

        Visibility SearchPlaceholderVisibility { get; }

        string FilterResultsInfo { get;  }
    }
}
