using FlatRedBall.Forms.MVVM;
using MultiplayerPlatformerDemo.GumRuntimes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerPlatformerDemo.ViewModels
{
    class IndividualJoinViewModel : ViewModel
    {
        public IndividualJoinComponentRuntime.JoinCategory JoinState
        {
            get => Get<IndividualJoinComponentRuntime.JoinCategory>();
            set => Set(value);
        }
    }
}
