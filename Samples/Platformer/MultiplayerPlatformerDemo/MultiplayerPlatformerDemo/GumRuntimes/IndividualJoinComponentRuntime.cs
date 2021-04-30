using MultiplayerPlatformerDemo.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiplayerPlatformerDemo.GumRuntimes
{
    public partial class IndividualJoinComponentRuntime
    {
        IndividualJoinViewModel ViewModel => BindingContext as IndividualJoinViewModel;
        partial void CustomInitialize () 
        {
            this.SetBinding(nameof(this.CurrentJoinCategoryState), nameof(ViewModel.JoinState));
        }
    }
}
