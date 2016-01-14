using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    public static class EnumExtensionMethods
    {
        // From StackOverflow:
        // http://stackoverflow.com/questions/4108828/generic-extension-method-to-see-if-an-enum-contains-a-flag
        public static bool HasFlag(this Enum variable, Enum value)
        {
            if (variable == null)
                return false;

            if (value == null)
                throw new ArgumentNullException("value");

            // Not as good as the .NET 4 version of this function, but should be good enough
            if (!Enum.IsDefined(variable.GetType(), value))
            {
                throw new ArgumentException(string.Format(
                    "Enumeration type mismatch.  The flag is of type '{0}', was expecting '{1}'.",
                    value.GetType(), variable.GetType()));
            }

            ulong num = Convert.ToUInt64(value);
            return ((Convert.ToUInt64(variable) & num) == num);

        }

    }


    public class EnumCompatability
    {

        public static Array GetValues(Type enumType)
        {
            List<object> tempList = new List<object>();

            foreach (var x in enumType.GetFields())
            {
                if (x.IsLiteral)
                {
                    tempList.Add(x);
                }
            }   

            return tempList.ToArray();
        }


    }
}
