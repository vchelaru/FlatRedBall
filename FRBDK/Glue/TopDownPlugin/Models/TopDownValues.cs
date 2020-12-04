using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TopDownPlugin.Models
{
    public enum InheritOrOverwrite
    {
        Inherit,
        Overwrite
    }

    public class TopDownValues
    {
        public string Name { get; set; }

        public bool UsesAcceleration { get; set; } = true;

        public float MaxSpeed { get; set; }
        public float AccelerationTime { get; set; }
        public float DecelerationTime { get; set; }
        public bool UpdateDirectionFromVelocity { get; set; } = true;

        public bool IsUsingCustomDeceleration { get; set; } = false;

        public float CustomDecelerationValue { get; set; } = 100;

        public int InheritOrOverwriteAsInt { get; set; }

        public InheritOrOverwrite InheritOrOverwrite => (InheritOrOverwrite)InheritOrOverwriteAsInt;

        // If adding properties here, modify:
        // * MainController.RefreshTopDownValues
        // * TopDownValuesViewModel.SetFrom
        // * TopDownValuesViewModel.ToValues
        // It might be good to find all references to an existing property to make sure it's covered

        public Dictionary<string, object> AdditionalValues
        {
            get; private set;
        } = new Dictionary<string, object>();
    }
}
