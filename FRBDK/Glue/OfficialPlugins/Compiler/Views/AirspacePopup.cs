using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;

namespace OfficialPlugins.GameHost.Views
{

    public class AirspacePopup : Popup
    {
        public static readonly DependencyProperty IsTopmostProperty =
            DependencyProperty.Register("IsTopmost",
                                        typeof(bool),
                                        typeof(AirspacePopup),
                                        new FrameworkPropertyMetadata(false, OnIsTopmostChanged));

        public static readonly DependencyProperty FollowPlacementTargetProperty =
            DependencyProperty.RegisterAttached("FollowPlacementTarget",
                                                typeof(bool),
                                                typeof(AirspacePopup),
                                                new UIPropertyMetadata(false));

        public static readonly DependencyProperty AllowOutsideScreenPlacementProperty =
            DependencyProperty.RegisterAttached("AllowOutsideScreenPlacement",
                                                typeof(bool),
                                                typeof(AirspacePopup),
                                                new UIPropertyMetadata(false));

        public static readonly DependencyProperty ParentWindowProperty =
            DependencyProperty.RegisterAttached("ParentWindow",
                                                typeof(Window),
                                                typeof(AirspacePopup),
                                                new UIPropertyMetadata(null, ParentWindowPropertyChanged));

        private static void OnIsTopmostChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            AirspacePopup airspacePopup = source as AirspacePopup;
            airspacePopup.SetTopmostState(airspacePopup.IsTopmost);
        }

        private static void ParentWindowPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            AirspacePopup airspacePopup = source as AirspacePopup;
            airspacePopup.ParentWindowChanged();
        }

        private bool? m_appliedTopMost;
        private bool m_alreadyLoaded;
        private Window m_parentWindow;

        public AirspacePopup()
        {
            Loaded += OnPopupLoaded;
            Unloaded += OnPopupUnloaded;

            DependencyPropertyDescriptor descriptor = DependencyPropertyDescriptor.FromProperty(PlacementTargetProperty, typeof(AirspacePopup));
            descriptor.AddValueChanged(this, PlacementTargetChanged);
        }

        public bool IsTopmost
        {
            get { return (bool)GetValue(IsTopmostProperty); }
            set { SetValue(IsTopmostProperty, value); }
        }
        public bool FollowPlacementTarget
        {
            get { return (bool)GetValue(FollowPlacementTargetProperty); }
            set { SetValue(FollowPlacementTargetProperty, value); }
        }
        public bool AllowOutsideScreenPlacement
        {
            get { return (bool)GetValue(AllowOutsideScreenPlacementProperty); }
            set { SetValue(AllowOutsideScreenPlacementProperty, value); }
        }
        public Window ParentWindow
        {
            get { return (Window)GetValue(ParentWindowProperty); }
            set { SetValue(ParentWindowProperty, value); }
        }

        private void ParentWindowChanged()
        {
            if (ParentWindow != null)
            {
                ParentWindow.LocationChanged += (sender, e2) =>
                {
                    UpdatePopupPosition();
                };
                ParentWindow.SizeChanged += (sender, e2) =>
                {
                    UpdatePopupPosition();
                };
            }
        }
        private void PlacementTargetChanged(object sender, EventArgs e)
        {
            FrameworkElement placementTarget = this.PlacementTarget as FrameworkElement;
            if (placementTarget != null)
            {
                placementTarget.SizeChanged += (sender2, e2) =>
                {
                    UpdatePopupPosition();
                };
            }
        }

        private void UpdatePopupPosition()
        {
            FrameworkElement placementTarget = this.PlacementTarget as FrameworkElement;
            FrameworkElement child = this.Child as FrameworkElement;

            if (PresentationSource.FromVisual(placementTarget) != null &&
                AllowOutsideScreenPlacement == true)
            {
                double leftOffset = CutLeft(placementTarget);
                double topOffset = CutTop(placementTarget);
                double rightOffset = CutRight(placementTarget);
                double bottomOffset = CutBottom(placementTarget);
                Debug.WriteLine(bottomOffset);
                this.Width = Math.Max(0, Math.Min(leftOffset, rightOffset) + placementTarget.ActualWidth);
                this.Height = Math.Max(0, Math.Min(topOffset, bottomOffset) + placementTarget.ActualHeight);

                if (child != null)
                {
                    child.Margin = new Thickness(leftOffset, topOffset, rightOffset, bottomOffset);
                }
            }
            if (FollowPlacementTarget == true)
            {
                this.HorizontalOffset += 0.01;
                this.HorizontalOffset -= 0.01;
            }
        }
        private double CutLeft(FrameworkElement placementTarget)
        {
            Point point = placementTarget.PointToScreen(new Point(0, placementTarget.ActualWidth));
            return Math.Min(0, point.X);
        }
        private double CutTop(FrameworkElement placementTarget)
        {
            Point point = placementTarget.PointToScreen(new Point(placementTarget.ActualHeight, 0));
            return Math.Min(0, point.Y);
        }
        private double CutRight(FrameworkElement placementTarget)
        {
            Point point = placementTarget.PointToScreen(new Point(0, placementTarget.ActualWidth));
            point.X += placementTarget.ActualWidth;
            return Math.Min(0, SystemParameters.VirtualScreenWidth - (Math.Max(SystemParameters.VirtualScreenWidth, point.X)));
        }
        private double CutBottom(FrameworkElement placementTarget)
        {
            Point point = placementTarget.PointToScreen(new Point(placementTarget.ActualHeight, 0));
            point.Y += placementTarget.ActualHeight;
            return Math.Min(0, SystemParameters.VirtualScreenHeight - (Math.Max(SystemParameters.VirtualScreenHeight, point.Y)));
        }

        private void OnPopupLoaded(object sender, RoutedEventArgs e)
        {
            if (m_alreadyLoaded)
                return;

            m_alreadyLoaded = true;

            if (Child != null)
            {
                Child.AddHandler(PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(OnChildPreviewMouseLeftButtonDown), true);
            }

            m_parentWindow = Window.GetWindow(this);

            if (m_parentWindow == null)
                return;

            m_parentWindow.Activated += OnParentWindowActivated;
            m_parentWindow.Deactivated += OnParentWindowDeactivated;
        }

        private void OnPopupUnloaded(object sender, RoutedEventArgs e)
        {
            if (m_parentWindow == null)
                return;
            m_parentWindow.Activated -= OnParentWindowActivated;
            m_parentWindow.Deactivated -= OnParentWindowDeactivated;
        }

        private void OnParentWindowActivated(object sender, EventArgs e)
        {
            SetTopmostState(true);
        }

        private void OnParentWindowDeactivated(object sender, EventArgs e)
        {
            if (IsTopmost == false)
            {
                SetTopmostState(IsTopmost);
            }
        }

        private void OnChildPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SetTopmostState(true);
            if (!m_parentWindow.IsActive && IsTopmost == false)
            {
                m_parentWindow.Activate();
            }
        }

        protected override void OnOpened(EventArgs e)
        {
            SetTopmostState(IsTopmost);
            base.OnOpened(e);
        }

        private void SetTopmostState(bool isTop)
        {
            // Don’t apply state if it’s the same as incoming state
            if (m_appliedTopMost.HasValue && m_appliedTopMost == isTop)
            {
                return;
            }

            if (Child == null)
                return;

            var hwndSource = (PresentationSource.FromVisual(Child)) as HwndSource;

            if (hwndSource == null)
                return;
            var hwnd = hwndSource.Handle;

            RECT rect;

            if (!GetWindowRect(hwnd, out rect))
                return;

            Debug.WriteLine("setting z-order " + isTop);

            if (isTop)
            {
                SetWindowPos(hwnd, HWND_TOPMOST, rect.Left, rect.Top, (int)Width, (int)Height, TOPMOST_FLAGS);
            }
            else
            {
                // Z-Order would only get refreshed/reflected if clicking the
                // the titlebar (as opposed to other parts of the external
                // window) unless I first set the popup to HWND_BOTTOM
                // then HWND_TOP before HWND_NOTOPMOST
                SetWindowPos(hwnd, HWND_BOTTOM, rect.Left, rect.Top, (int)Width, (int)Height, TOPMOST_FLAGS);
                SetWindowPos(hwnd, HWND_TOP, rect.Left, rect.Top, (int)Width, (int)Height, TOPMOST_FLAGS);
                SetWindowPos(hwnd, HWND_NOTOPMOST, rect.Left, rect.Top, (int)Width, (int)Height, TOPMOST_FLAGS);
            }

            m_appliedTopMost = isTop;
        }

        #region P/Invoke imports & definitions
#pragma warning disable 1591 //Xml-doc
#pragma warning disable 169 //Never used-warning
        // ReSharper disable InconsistentNaming
        // Imports etc. with their naming rules

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT

        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X,
        int Y, int cx, int cy, uint uFlags);

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        static readonly IntPtr HWND_TOP = new IntPtr(0);
        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        private const UInt32 SWP_NOSIZE = 0x0001;
        const UInt32 SWP_NOMOVE = 0x0002;
        const UInt32 SWP_NOZORDER = 0x0004;
        const UInt32 SWP_NOREDRAW = 0x0008;
        const UInt32 SWP_NOACTIVATE = 0x0010;

        const UInt32 SWP_FRAMECHANGED = 0x0020; /* The frame changed: send WM_NCCALCSIZE */
        const UInt32 SWP_SHOWWINDOW = 0x0040;
        const UInt32 SWP_HIDEWINDOW = 0x0080;
        const UInt32 SWP_NOCOPYBITS = 0x0100;
        const UInt32 SWP_NOOWNERZORDER = 0x0200; /* Don’t do owner Z ordering */
        const UInt32 SWP_NOSENDCHANGING = 0x0400; /* Don’t send WM_WINDOWPOSCHANGING */

        const UInt32 TOPMOST_FLAGS =
            SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_NOSIZE | SWP_NOMOVE | SWP_NOREDRAW | SWP_NOSENDCHANGING;

        // ReSharper restore InconsistentNaming
#pragma warning restore 1591
#pragma warning restore 169
        #endregion
    }

}