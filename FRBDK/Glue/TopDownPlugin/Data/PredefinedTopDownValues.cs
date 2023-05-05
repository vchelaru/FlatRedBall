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

            var defaultValues = new TopDownValuesViewModel
            {
                Name = "Default",
                MovementMode = TopDownValuesViewModel.ImmediateOrAccelerate.Accelerate,
                MaxSpeed = 300,
                AccelerationTime = .5f,
                DecelerationTime = .25f,
                ShouldChangeMovementDirection = TopDownValuesViewModel.VelocityChangeMode.UpdateFromInput,
                // intentionally don't set IsCustomDecelerationChecked to true. The user may not want that, but if they
                // do, we should have CustomDecelerationValue set to a useful default
                CustomDecelerationValue = 1000
            };

            topDownValues.Add(defaultValues.Name, defaultValues);
        }

        public static TopDownValuesViewModel GetValues(string name)
        {
            var toReturn = topDownValues[name].Clone();
            return toReturn;
        }
    }
}
