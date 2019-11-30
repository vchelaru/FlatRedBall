using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TopDownPlugin.Data
{
    public class MovementValueAnimations
    {
        public string MovementValuesName;

        public List<AnimationSetModel> AnimationSets
            = new List<AnimationSetModel>();

        public override string ToString()
        {
            return $"{MovementValuesName} ({AnimationSets.Count} sets)";
        }
    }
}
