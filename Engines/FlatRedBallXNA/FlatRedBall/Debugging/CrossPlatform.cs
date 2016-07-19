using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Debugging
{
    public enum Platform
    {
        WindowsDesktop,
        WindowsRt,
        iOS,
        Android,
        Xbox360,
        Uwp
    }


    public static class CrossPlatform
    {
        static List<Platform> platformBasedRestrictions = new List<Platform>();



        public static void AddPlatformBasedRestrictions(Platform platform)
        {
            platformBasedRestrictions.Add(platform);
        }

        public static bool ShouldApplyRestrictionsFor(Platform platform)
        {
            return platformBasedRestrictions.Contains(platform);
        }
    }
}
