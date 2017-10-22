using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueTestProject.TestFramework
{
    public static class ShouldExtensions
    {
        public static void ShouldBe<T>(this T thisObject, T desired, string because = null)
        {
            bool equals = EqualityComparer<T>.Default.Equals(thisObject, desired);
            

            if(!equals)
            {
                string message = $"Should be {desired} but was instead {thisObject}";

                if(!string.IsNullOrEmpty(because))
                {
                    message += $" : {because}";
                }
                throw new Exception(message);
            }
        }

        public static void ShouldNotBe<T>(this T thisObject, T undesired, string because = null)
        {
            bool equals = EqualityComparer<T>.Default.Equals(thisObject, undesired);

            if (equals)
            {
                string message = $"Should not be {undesired}";

                if (!string.IsNullOrEmpty(because))
                {
                    message += $" : {because}";
                }

                throw new Exception(message);
            }
        }


    }
}
