using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.TypeConversions
{
    public struct ConversionCode
    {
        public string FromType;
        public string ToType;
        public string ConversionString;
    }

    public class CustomTypeConverter
    {
        List<ConversionCode> mConversionCodeLibrary = new List<ConversionCode>();



        public string GetConversion(string fromType, string toType, string valueToInsert)
        {
            foreach (ConversionCode conversionCode in mConversionCodeLibrary)
            {
                if (conversionCode.FromType == fromType &&
                    conversionCode.ToType == toType)
                {
                    return conversionCode.ConversionString.Replace("value", valueToInsert);
                }
            }

            return null;

        }

        public void AddConversion(string fromType, string toType, string conversionString)
        {
            ConversionCode code = new ConversionCode();
            code.FromType = fromType;
            code.ToType = toType;
            code.ConversionString = conversionString;

            mConversionCodeLibrary.Add(code);
        }


    }
}
