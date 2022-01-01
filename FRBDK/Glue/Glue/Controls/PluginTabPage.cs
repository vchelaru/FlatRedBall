using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FlatRedBall.Glue.MVVM;
//using System.Windows.Forms;
using FlatRedBall.Glue.Plugins;
using GlueFormsCore.Controls;
using GlueFormsCore.ViewModels;

namespace FlatRedBall.Glue.Controls
{
    public class PluginTabPage : System.Windows.Controls.TabItem
    {
        #region Fields/properties

        TextBlock textBlock;

        Button closeButton;

        MenuItem moveMenuItem;

        public event Action<TabLocation> MoveToTabSelected;

        public string Title
        {
            get => textBlock.Text;
            set => textBlock.Text = value;
        }

        public TabContainerViewModel ParentTabControl
        {
            get; set;
        }

        public DateTime LastTimeClicked
        {
            get;
            private set;
        }

        public bool DrawX
        {
            get => closeButton.Visibility == System.Windows.Visibility.Visible;
            set => closeButton.Visibility = value.ToVisibility();
        }

        #endregion

        #region Events/Delegates

        public delegate void ClosedByUserDelegate(object sender);

        public event ClosedByUserDelegate ClosedByUser;

        public Action TabSelected;

        #endregion

        public PluginTabPage() : base()
        {
            this.Resources = MainPanelControl.ResourceDictionary;
            //var backgroundBrush = MainPanelControl.ResourceDictionary["BlackBrush"];
            //this.Background = (System.Windows.Media.Brush)backgroundBrush;

            Style style = this.TryFindResource("TabItemStyle") as Style;
            if(style != null)
            {
                this.Style = style;
            }

            var stackPanel = new StackPanel();
            stackPanel.Orientation = Orientation.Horizontal;
            stackPanel.MouseDown += HandleMouseDown;
            this.Header = stackPanel;
            textBlock = new TextBlock();
            textBlock.Margin = new Thickness(3);
            stackPanel.Children.Add(textBlock);
            //this.ContextMenu = new ContextMenu();

            closeButton = new Button();
            var innerTextBlock = new TextBlock();
            innerTextBlock.Text = "x";
            innerTextBlock.Margin = new Thickness(0, -7, 0, 0);
            closeButton.Content = innerTextBlock;
            closeButton.Margin = new System.Windows.Thickness(7, 0, 0, 0);
            closeButton.FontWeight = FontWeights.Bold;
            closeButton.Click += (not, used) => ClosedByUser?.Invoke(this);
            closeButton.Height = 10;
            //closeButton.Background = new SolidColorBrush(Colors.Red);
            closeButton.VerticalContentAlignment = VerticalAlignment.Top;
            stackPanel.Children.Add(closeButton);


            this.VerticalContentAlignment = VerticalAlignment.Stretch;

            moveMenuItem = new MenuItem();
            moveMenuItem.Header = "Move to";
            this.ContextMenu = new ContextMenu();
            this.ContextMenu.Items.Add(moveMenuItem);

            //moveToMenuItem = this.ContextMenu.MenuItems.Add("MoveTo");

            //closeMenuItem = new MenuItem("Close");
            //closeMenuItem.Click += (not, used) => RightClickCloseClicked?.Invoke(this, null);

        }

        protected override void OnSelected(RoutedEventArgs e)
        {
            base.OnSelected(e);
            RecordLastClick();
        }

        private void HandleMouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton == MouseButton.Middle && 
                e.ButtonState == MouseButtonState.Pressed &&
                DrawX)
            {
                ClosedByUser?.Invoke(this);
            }
        }

        public void RefreshRightClickCommands()
        {
            RefreshMoveToCommands();
        }

        private void RefreshMoveToCommands()
        {
            var parent = this.Parent;

            moveMenuItem.Items.Clear();

            void Add(string text, TabLocation tabLocation)
            {
                var menuItem = new MenuItem();
                menuItem.Header = text;
                menuItem.Click += (not, used) => MoveToTabSelected?.Invoke(tabLocation);
                var image = new System.Windows.Controls.Image();
                string pngName = $"{tabLocation}Tab.png";
                image.Source = new BitmapImage(new Uri($@"pack://application:,,,/Resources/Icons/MoveTabs/{pngName}"));
                menuItem.Icon = image;
                moveMenuItem.Items.Add(menuItem);
            }

            if (ParentTabControl != PluginManager.TabControlViewModel.TopTabItems)
            {
                Add("Top Tab", TabLocation.Top);
            }

            if (ParentTabControl != PluginManager.TabControlViewModel.LeftTabItems)
            {
                Add("Left Tab", TabLocation.Left);
            }

            if (ParentTabControl != PluginManager.TabControlViewModel.CenterTabItems)
            {
                Add("Center Tab", TabLocation.Center);
            }

            if (ParentTabControl != PluginManager.TabControlViewModel.RightTabItems)
            {
                Add("Right Tab", TabLocation.Right);
            }

            if (ParentTabControl != PluginManager.TabControlViewModel.BottomTabItems)
            {
                Add("Bottom Tab", TabLocation.Bottom);
            }

        }

        public void CloseTabByUser()
        {
            ClosedByUser?.Invoke(this);
        }

        public void RecordLastClick()
        {
            LastTimeClicked = DateTime.Now;
            ParentTabControl?.SetTabForCurrentType(this);
        }

        public override string ToString()
        {
            return Title;
        }
    }
}
