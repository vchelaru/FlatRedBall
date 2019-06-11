using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueSaveClasses.Models.TypeConverters
{
    public interface IConverter
    {
        object Convert(object toConvert);
        object ConvertBack(object converted);
    }
}
