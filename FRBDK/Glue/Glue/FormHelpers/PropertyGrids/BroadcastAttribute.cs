using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.FormHelpers.PropertyGrids
{
    public enum BroadcastStaticOrInstance
    {
        Internal,
        Instance
    }

    public class BroadcastAttribute : Attribute
    {

        public BroadcastStaticOrInstance StaticOrInstance
        {
            get;
            set;
        }

        public BroadcastAttribute(BroadcastStaticOrInstance staticOrInstance)
        {
            StaticOrInstance = staticOrInstance;
        }


    }
}
