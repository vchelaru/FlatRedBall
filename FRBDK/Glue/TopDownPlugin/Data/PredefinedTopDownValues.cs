using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TopDownPlugin.ViewModels;

namespace TopDownPlugin.Data
{
    public static class PredefinedTopDownValues
    {
        static Dictionary<string, TopDownValuesViewModel> topDownValues =
            new Dictionary<string, TopDownValuesViewModel>();

        static PredefinedTopDownValues()
        {
            var unnamed = new TopDownValuesViewModel
            {
                Name = "Unnamed"
            };
            topDownValues.Add(unnamed.Name, unnamed);
        }

        public static TopDownValuesViewModel GetValues(string name)
        {
            var toReturn = topDownValues[name].Clone();
            return toReturn;
        }
    }
}
