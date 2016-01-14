namespace System.Windows.Forms
{
    public static class WindowsFormEx
    {
        public static void EnsureOnScreen(this System.Windows.Forms.Form form)
        {
            var screen = Screen.FromControl(form);
            System.Drawing.Point newLocation = form.Location;

            if (form.Bounds.Right > screen.Bounds.Right)
                newLocation.X = screen.Bounds.Right - form.Width - 5;
            if (form.Bounds.Bottom > screen.Bounds.Bottom)
                newLocation.Y = screen.Bounds.Bottom - form.Height - 5;

            form.Location = newLocation;
        }
    }
}
