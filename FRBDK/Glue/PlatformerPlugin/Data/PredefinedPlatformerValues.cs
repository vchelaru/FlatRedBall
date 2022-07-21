using FlatRedBall.PlatformerPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.PlatformerPlugin.Data
{
    public static class PredefinedPlatformerValues
    {
        static Dictionary<string, PlatformerValuesViewModel> platformerValues =
            new Dictionary<string, PlatformerValuesViewModel>();
        static PredefinedPlatformerValues()
        {
            {
                var unnamed = new PlatformerValuesViewModel
                {
                    Name = "Unnamed",
                    // Even though this is empty, we still want to apply gravity and max fall speed. Otherwise, this
                    // set of values could result in weird flickering between on ground and not on ground.
                    Gravity = 900,
                    MaxFallSpeed = 500,
                };
                platformerValues.Add(unnamed.Name, unnamed);
            }


            {
                var defaultGround = new PlatformerValuesViewModel
                {
                    Name = "Ground",

                    MaxSpeedX = 160,
                    AccelerationTimeX = .20f,
                    DecelerationTimeX = .15f,
                    IsImmediate = false,
                    JumpVelocity = 260,
                    JumpApplyByButtonHold = true,
                    JumpApplyLength = .17f,
                    Gravity = 900,
                    MaxFallSpeed = 500,
                    CanFallThroughCloudPlatforms = true,
                    CloudFallThroughDistance = 16,

                    MoveSameSpeedOnSlopes = false,
                    UphillFullSpeedSlope = 0,
                    UphillStopSpeedSlope= 60,
                    DownhillFullSpeedSlope = 0,
                    DownhillMaxSpeedSlope = 60,
                    DownhillMaxSpeedBoostPercentage = 50
                };
                platformerValues.Add(defaultGround.Name, defaultGround);
            }

            {
                var defaultInAir = new PlatformerValuesViewModel
                {
                    Name = "Air",

                    MaxSpeedX = 160,
                    AccelerationTimeX = 1,
                    DecelerationTimeX = 1,
                    IsImmediate = false,
                    JumpVelocity = 0,
                    Gravity = 1000,
                    MaxFallSpeed = 400,
                    CanFallThroughCloudPlatforms = false,

                    MoveSameSpeedOnSlopes = true
            
            };
                platformerValues.Add(defaultInAir.Name, defaultInAir);
            }
        }

        public static PlatformerValuesViewModel GetValues(string name)
        {
            var toReturn = platformerValues[name].Clone();
            return toReturn;
        }
    }
}
