using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Packaging;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Navigation;

namespace GlueFormsCore.Extensions
{
    public static class WpfExtensions
    {
        // from 
        // 

        /// <summary>
        ///     Intent:  
        ///     - Shift the window onto the visible screen.
        ///     - Shift the window away from overlapping the task bar.
        /// </summary>
        public static void ShiftWindowOntoScreen(this Window window)
        {
            var heightToUse = window.Height;
            if (double.IsNaN(heightToUse))
            {
                heightToUse = 50; // just assume it has *some* height...
            }
            var screen = Screen.FromPoint(Cursor.Position);
            var bounds = screen.WorkingArea;
            // Note that "window.BringIntoView()" does not work.                            
            if (window.Top < bounds.Top)
            {
                window.Top = bounds.Top;
            }

            if (window.Left < bounds.Left)
            {
                window.Left = bounds.Left;
            }

            if (window.Left + window.Width > bounds.Right)
            {
                window.Left = bounds.Right - window.Width;
            }

            if (window.Top + heightToUse > bounds.Bottom)
            {
                window.Top = bounds.Bottom - heightToUse - 1;
            }
        }
    }
    public static class UserControlExtension
    {
        public static void LoadViewFromUri(this UserControl userControl, string baseUri)
        {
            try
            {
                var resourceLocater = new Uri(baseUri, UriKind.Relative);
                var exprCa = (PackagePart)typeof(System.Windows.Application).GetMethod("GetResourceOrContentPart", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { resourceLocater });
                var stream = exprCa.GetStream();
                var uri = new Uri((Uri)typeof(BaseUriHelper).GetProperty("PackAppBaseUri", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null, null), resourceLocater);
                var parserContext = new ParserContext
                {
                    BaseUri = uri
                };
                typeof(XamlReader).GetMethod("LoadBaml", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { stream, parserContext, userControl, true });
            }
            catch (Exception)
            {
                //log
            }
        }
    }
}
