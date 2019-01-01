using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TileGraphicsPlugin.ViewModels
{
    public class TiledMapEntityCreationViewModel : ViewModel
    {
        public bool CreateEntitiesInGeneratedCode
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        public string GeneratedEntitiesInformation
        {
            get
            {
                return null;
            }
        }
    }
}
