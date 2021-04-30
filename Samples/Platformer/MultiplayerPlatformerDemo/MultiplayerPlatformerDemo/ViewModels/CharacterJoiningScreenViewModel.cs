using FlatRedBall.Forms.MVVM;
using MultiplayerPlatformerDemo.GumRuntimes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerPlatformerDemo.ViewModels
{
    class CharacterJoiningScreenViewModel : ViewModel
    {
        public string MainText
        {
            get
            {
                if(IndividualJoinViewModels.All(item => item.JoinState == IndividualJoinComponentRuntime.JoinCategory.NotPluggedIn))
                {
                    return "No controllers plugged in - plug in at least one controller";
                }
                else if(IndividualJoinViewModels.Any(item => item.JoinState == IndividualJoinComponentRuntime.JoinCategory.PluggedInNotJoined) && 
                    !IndividualJoinViewModels.Any(item => item.JoinState == IndividualJoinComponentRuntime.JoinCategory.Joined))
                {
                    return "Press A to join, or plug in more controllers";
                }
                else if(IndividualJoinViewModels.Any(item => item.JoinState == IndividualJoinComponentRuntime.JoinCategory.Joined ))
                {
                    return "Press Start to begin";
                }
                else
                {
                    return "??";
                }
            }
        }

        public IndividualJoinViewModel[] IndividualJoinViewModels
        {
            get;
            private set;
        } = new IndividualJoinViewModel[4];

        public CharacterJoiningScreenViewModel()
        {
            for(int i = 0; i < IndividualJoinViewModels.Length; i++)
            {
                var individualVm = new IndividualJoinViewModel();

                // This ties the MainText to a change on the individual VM's JoinState
                individualVm.SetPropertyChanged(nameof(individualVm.JoinState), () => NotifyPropertyChanged(nameof(MainText)));

                IndividualJoinViewModels[i] = individualVm;
            }
        }
    }
}
