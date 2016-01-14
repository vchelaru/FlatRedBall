using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using Microsoft.Xna.Framework;

namespace FlatRedBall.Glue.GuiDisplay
{
    public class AvailableColorTypeConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {

            return true;
        }


        List<string> stringToReturn = new List<string>();

        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {

            stringToReturn.Clear();

            PropertyInfo[] propertyArray =
                typeof(Color).GetProperties(BindingFlags.Static | BindingFlags.Public);

            foreach (PropertyInfo property in propertyArray)
            {
                stringToReturn.Add(property.Name);
            }



            return new StandardValuesCollection(stringToReturn);
        }

        public static string GetStandardColorNameFrom(Color color)
        {
            PropertyInfo[] propertyArray =
                typeof(Color).GetProperties(BindingFlags.Static | BindingFlags.Public);

            foreach (PropertyInfo property in propertyArray)
            {
                object potentialColor = property.GetValue(null, null);

                if (potentialColor is Color)
                {
                    Color standardColor = (Color)potentialColor;
                    if (standardColor == color)
                    {
                        return property.Name;
                    }
                }
            }
            return null;
        }
    }
}
