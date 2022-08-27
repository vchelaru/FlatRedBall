namespace System.Windows.Forms
{
    public static class WindowsFormEx
    {
        public static void EnsureOnScreen(this System.Windows.Forms.Form form)
        {
            var screen = Screen.FromControl(form).WorkingArea;
            System.Drawing.Point newLocation = form.Location;

            if (form.Bounds.Right > screen.Right)
                newLocation.X = screen.Right - form.Width - 5;
            if (form.Bounds.Bottom > screen.Bottom)
                newLocation.Y = screen.Bottom - form.Height - 5;
            if (form.Bounds.Left < 0)
                newLocation.X = 0;

            form.Location = newLocation;
        }
    }
}
