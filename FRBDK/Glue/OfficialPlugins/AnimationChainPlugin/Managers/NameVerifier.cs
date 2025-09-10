using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficialPlugins.AnimationChainPlugin.ViewModels;

namespace OfficialPlugins.AnimationChainPlugin.Managers;

internal class NameVerifier
{
    public bool IsAnimationNameValid(string newName, AnimationChainViewModel animation, AchxViewModel achxViewModel, out string whyNotValid)
    {
        whyNotValid = string.Empty;
        if (string.IsNullOrWhiteSpace(newName))
        {
            whyNotValid = "Name cannot be empty";
        }
        else if(newName.Contains(" "))
        {
            whyNotValid = "Name cannot contain spaces";
        }
        else if(achxViewModel.VisibleRoot.Any(item => item != animation && item.Name == newName))
        {
            whyNotValid = "An animation with this name already exists";
        }

        return whyNotValid == string.Empty;
    }
}
