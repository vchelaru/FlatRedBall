using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Globalization;

namespace FlatRedBall.AnimationEditorForms.Converters
{
    public class SpriteSheetCellWidthConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            // Allow typing in of values
            return false;
        }

        //public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        //{
        //    string asString = (string)value;

        //    if (asString.Contains("cells") && SelectedState.Self.SelectedTexture != null)
        //    {
        //        asString = asString.Replace(" cells", "");

        //        int cellsWide = int.Parse(asString);

        //        return SelectedState.Self.SelectedTexture.Width / cellsWide;
        //    }
        //    else
        //    {
        //        return int.Parse((string)value);
        //    }
        //}

        //public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        //{
        //    return value.ToString();
        //}

        //public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        //{
        //    return true;
        //}

        //public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        //{
        //    return true;
        //}

        List<string> values = new List<string>();
        public override StandardValuesCollection
                     GetStandardValues(ITypeDescriptorContext context)
        {

            values.Clear();

            for (int i = 0; i < 32; i++)
            {
                values.Add((i + 1) + " cells");
            }

            StandardValuesCollection svc = new StandardValuesCollection(values);

            return svc;
        } 
    }
}
