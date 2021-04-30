using MultiplayerPlatformerDemo.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiplayerPlatformerDemo.GumRuntimes
{
    public partial class CharacterJoiningScreenGumRuntime
    {
        CharacterJoiningScreenViewModel ViewModel => BindingContext as CharacterJoiningScreenViewModel;
        partial void CustomInitialize () 
        {
            this.InstructionTextInstance.SetBinding(
                nameof(InstructionTextInstance.Text),
                nameof(ViewModel.MainText));
        }
    }
}
