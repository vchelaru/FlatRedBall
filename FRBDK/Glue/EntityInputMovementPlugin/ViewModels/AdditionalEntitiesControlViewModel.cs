using EntityInputMovementPlugin.ViewModels;
using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TopDownPlugin.ViewModels
{
    public class AdditionalEntitiesControlViewModel : ViewModel
    {
        public MovementType MovementType
        {
            get => Get<MovementType>();
            set => Set(value);
        }

        [DependsOn(nameof(MovementType))]
        public bool IsNoneRadioChecked
        {
            get => MovementType == MovementType.None;
            set
            {
                if (value)
                {
                    MovementType = MovementType.None;
                }

            }
        }

        [DependsOn(nameof(MovementType))]
        public bool IsTopDownRadioChecked
        {
            get => MovementType == MovementType.TopDown;
            set
            {
                if (value)
                {
                    MovementType = MovementType.TopDown;
                }
            }
        }

        [DependsOn(nameof(MovementType))]
        public bool IsPlatformerRadioChecked
        {
            get => MovementType == MovementType.Platformer;
            set
            {
                if (value)
                {
                    MovementType = MovementType.Platformer;
                }
            }
        }


    }
}
