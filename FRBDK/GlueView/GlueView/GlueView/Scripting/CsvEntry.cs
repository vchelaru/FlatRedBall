using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.IO.Csv;
using GlueViewOfficialPlugins.Scripting;

namespace GlueView.Scripting
{
    public class CsvEntry
    {
        public RuntimeCsvRepresentation RuntimeCsvRepresentation
        {
            get;
            set;
        }

        public int StartIndex
        {
            get;
            set;
        }

        public int Count
        {
            get;
            set;
        }


        public object GetValue(string name)
        {
            int columnIndex = -1;

            for (int i = 0; i < RuntimeCsvRepresentation.Headers.Length; i++)
            {
                if (RuntimeCsvRepresentation.Headers[i].Name == name)
                {
                    columnIndex = i;
                    break;
                }
            }

            if (columnIndex == -1)
            {
                return null;
            }
            else
            {
                string valueAsString = RuntimeCsvRepresentation.Records[StartIndex][columnIndex];
                return CsvParser.ConvertValueToType(valueAsString, RuntimeCsvRepresentation.Headers[columnIndex].OriginalText);
            }
            //return null;
        }
    }
}
