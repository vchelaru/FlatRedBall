using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TopDownPlugin.Models
{
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

        // If adding properties here, modify the MainController.GetCsvValues
        // to also consider the new properties

        public Dictionary<string, object> AdditionalValues
        {
            get; private set;
        } = new Dictionary<string, object>();
    }
}
