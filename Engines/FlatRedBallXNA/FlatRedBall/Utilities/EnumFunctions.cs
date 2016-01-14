using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace FlatRedBall.Utilities
{
    public static class EnumFunctions
    {

        public static string[] GetNames(Type enumType)
        {
            // Use public | static to remove the "value__" field
            FieldInfo[] fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
            string[] listToReturn = new string[fields.Length];

            for(int i= 0; i < fields.Length; i++)
            {
                listToReturn[i] = fields[i].Name;
            }

            return listToReturn;
        }

        public static bool Contains(this int enumToCheck, int enumToCheckFor)
        {
            return (enumToCheck & enumToCheckFor) == enumToCheckFor;
        }
    }
}
