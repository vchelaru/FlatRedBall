using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RacingPlugin.ViewModels
{
    public class RacingEntityViewModel : PropertyListContainerViewModel
    {
        public bool IsRacingEntity
        {
            get => Get<bool>();
            set => SetAndPersist(value);
        }
    }
}
