// *
// * Copyright (C) 2008 Roger Alsing : http://www.RogerAlsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace Alsing.SourceCode
{
    public class TextStyleUIEditor : UITypeEditor
    {
        private IWindowsFormsEditorService edSvc;

        public override object EditValue(ITypeDescriptorContext context,
                                         IServiceProvider provider, object value)
        {
            if (context != null && context.Instance != null && provider != null)
            {
                edSvc = (IWindowsFormsEditorService) provider.GetService(typeof
                                                                             (IWindowsFormsEditorService));


                if (edSvc != null)
                {
                    var style = (TextStyle) value;
                    using (var tsd = new TextStyleDesignerDialog(style)
                        )
                    {
                        context.OnComponentChanging();
                        if (edSvc.ShowDialog(tsd) == DialogResult.OK)
                        {
                            ValueChanged(this, EventArgs.Empty);
                            context.OnComponentChanged();
                            return style;
                        }
                    }
                }
            }

            return value;
        }


        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext
                                                               context)
        {
            return UITypeEditorEditStyle.Modal;
        }


        private void ValueChanged(object sender, EventArgs e)
        {
            if (edSvc != null) {}
        }

        public override void PaintValue(PaintValueEventArgs e)
        {
            var ts = (TextStyle) e.Value;
            using (var b = new SolidBrush(ts.BackColor))
            {
                e.Graphics.FillRectangle(b, e.Bounds);
            }

            FontStyle fs = FontStyle.Regular;
            if (ts.Bold)
                fs |= FontStyle.Bold;
            if (ts.Italic)
                fs |= FontStyle.Italic;
            if (ts.Underline)
                fs |= FontStyle.Underline;

            var f = new Font("arial", 8f, fs);


            using (var b = new SolidBrush(ts.ForeColor))
            {
                e.Graphics.DrawString("abc", f, b, e.Bounds);
            }

            f.Dispose();
        }

        public override bool GetPaintValueSupported
            (ITypeDescriptorContext context)
        {
            return true;
        }
    }
}