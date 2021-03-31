using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueSaveClasses.Models.TypeConverters
{
    public class InverterConverter : IConverter
    {
        public object Convert(object value)
        {
            if (value is bool)
            {
                return !((bool)value);
            }

            return false;
        }

        public object ConvertBack(object value)
        {
            return !((bool)value);
        }
    }
}
