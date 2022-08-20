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
using Glue;

namespace GlueFormsCore.Extensions
{
    public static class WpfExtensions
    {
        public static void MoveToCursor(this Window window, PresentationSource source = null)
        {
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;

            if(double.IsNaN(window.Width) || double.IsNaN(window.Height))
                try { window.UpdateLayout(); } catch { } //Can this throw exception? I don't know...

            double width = window.Width;
            if (double.IsNaN(width))
            {
                width = 64;
            }
            double height = window.Height;
            if (double.IsNaN(height))
            {
                // Let's just assume some small height so it doesn't appear down below the cursor:
                //height = 0;
                height = 64;
            }

            double mousePositionX = Control.MousePosition.X;
            double mousePositionY = Control.MousePosition.Y;

            if (source != null)
            {
                mousePositionX /= source.CompositionTarget.TransformToDevice.M11;
                mousePositionY /= source.CompositionTarget.TransformToDevice.M22;
            }

            window.Left = System.Math.Max(0, mousePositionX - width / 2);
            window.Top = mousePositionY - height / 2;

            window.ShiftWindowOntoScreen();
        }

        public static void MoveToMainWindowCenterAndSize(this Window window, float WidthAmount = 0.75f, float HeightAmount = 0.75f)
        {
            const float MinSize = 400f;

            //This isn't working... SetOwnerToMainGlueWindow(window);

            var mw = Glue.MainGlueWindow.Self;
            if(mw != null) {
                window.Width = mw.Width * WidthAmount;
                window.Height = mw.Height * HeightAmount;
                window.Left = mw.Left + ((mw.Width - window.Width) / 2);
                window.Top = mw.Top + ((mw.Height - window.Height) / 4);
                if(window.Width < MinSize) window.Width = MinSize;
                if(window.Height < MinSize) window.Height = MinSize;
            } else {
                window.Width = MinSize;
                window.Height = MinSize;
            }

            window.ShiftWindowOntoScreen();
        }

        /// <summary> Attempt to place dialog where wanted.  If large center main window otherwise at cursor </summary>
        //public static void MoveToPositionAndSize(this Window window, float? Left = null, float? Top = null, float? Width = null, float? Height = null) {
        //    //This isn't working... SetOwnerToMainGlueWindow(window);

        //}

        public static void SetOwnerToMainGlueWindow(this Window window)
        {
            if(MainGlueWindow.Self == null) return;
            try {
                //Why is this not setting window.Owner? (At least in the case of MapTextureButtonContainer.Button_Click)
                new System.Windows.Interop.WindowInteropHelper(window).Owner = MainGlueWindow.Self.Handle;
            } catch { }
        }

        /// <summary>
        ///     Intent:  
        ///     - Shift the window onto the visible screen.
        ///     - Shift the window away from overlapping the task bar.
        /// </summary>
        public static void ShiftWindowOntoScreen(this Window window)
        {
            if(double.IsNaN(window.Height))
                try { window.UpdateLayout(); } catch { } //Can this throw exception? I don't know...

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
                window.Top = bounds.Top + 5;
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
