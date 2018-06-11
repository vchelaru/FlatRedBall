using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.FormHelpers.StringConverters
{
    public abstract class TypeConverterWithNone : TypeConverter
    {
        public bool IncludeNoneOption { get; set; }
    }
}
