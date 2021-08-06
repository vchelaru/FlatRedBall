using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GlueCsvEditor.Styling
{
    public class ColoringLogic
    {
        internal void SetRowColor(System.Windows.Forms.DataGridViewRow row, string value)
        {

            if (string.IsNullOrEmpty(value))
            {
                row.DefaultCellStyle.BackColor = System.Drawing.Color.LightBlue;
            }
            else if(value.StartsWith("//"))
            {
                row.DefaultCellStyle.BackColor = System.Drawing.Color.LightGreen;
            }
            else
            {
                row.DefaultCellStyle.BackColor = System.Drawing.Color.White;
            }
        }
    }
}
