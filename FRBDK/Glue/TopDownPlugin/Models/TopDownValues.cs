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

        // If adding properties here, modify the MainController.GetCsvFalues
        // to also consider the new properties

        public Dictionary<string, object> AdditionalValues
        {
            get; private set;
        } = new Dictionary<string, object>();
    }
}
