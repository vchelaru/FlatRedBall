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

                // Max speed, AccelerationTime, and DecelerationTime have been
                // tuned to work well with a 480x360 resolution game
                // Update May 12, 2024
                // These values have been
                // adjusted to feel more "Link to the past" like.
                // They haven't really been compared with the game
                // itself, just eyeballed
                //MaxSpeed = 150,
                //AccelerationTime = .2f,
                //DecelerationTime = .1f,

                MaxSpeed = 76,
                AccelerationTime = .16f,
                DecelerationTime = .08f,


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
