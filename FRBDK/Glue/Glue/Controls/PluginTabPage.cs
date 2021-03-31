using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FlatRedBall.Glue.MVVM;
//using System.Windows.Forms;
using FlatRedBall.Glue.Plugins;
using GlueFormsCore.Controls;

namespace FlatRedBall.Glue.Controls
{
    public class PluginTabPage : 
        System.Windows.Controls.TabItem
        //TabPage
    {
        public delegate void ClosedByUserDelegate(object sender);
        public event ClosedByUserDelegate ClosedByUser;

        public Action TabSelected;

        TextBlock textBlock;
        //TextBlock closeX;
        Button closeButton;

        public string Title
        {
            get => textBlock.Text;
            set => textBlock.Text = value;
        }

        public object ParentTabControl
        {
            get; set;
        }

        public DateTime LastTimeClicked
        {
            get;
            set;
        }

        public bool DrawX
        {
            get => closeButton.Visibility == System.Windows.Visibility.Visible;
            set => closeButton.Visibility = value.ToVisibility();
        }

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

            //moveToMenuItem = this.ContextMenu.MenuItems.Add("MoveTo");

            //closeMenuItem = new MenuItem("Close");
            //closeMenuItem.Click += (not, used) => RightClickCloseClicked?.Invoke(this, null);

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
            RefreshCloseCommands();

            RefreshMoveToCommands();
        }

        private void RefreshCloseCommands()
        {
            //var alreadyContains = ContextMenu.MenuItems.Contains(closeMenuItem);
            //if(DrawX && alreadyContains == false)
            //{
            //    ContextMenu.MenuItems.Add(closeMenuItem);
            //}
            //if(!DrawX && alreadyContains)
            //{
            //    ContextMenu.MenuItems.Remove(closeMenuItem);
            //}

        }

        private void RefreshMoveToCommands()
        {
            //moveToMenuItem.MenuItems.Clear();
            //if (ParentTabControl != PluginManager.LeftTab)
            //{
            //    moveToMenuItem.MenuItems.Add("Left Tab", HandleMoveToLeftTab);
            //}

            //if (ParentTabControl != PluginManager.RightTab)
            //{
            //    moveToMenuItem.MenuItems.Add("Right Tab", HandleMoveToRightTab);
            //}

            //if (ParentTabControl != PluginManager.TopTab)
            //{
            //    moveToMenuItem.MenuItems.Add("Top Tab", HandleMoveToTopTab);
            //}

            //if (ParentTabControl != PluginManager.BottomTab)
            //{
            //    moveToMenuItem.MenuItems.Add("Bottom Tab", HandleMoveToBottomTab);
            //}

            //if (ParentTabControl != PluginManager.CenterTab)
            //{
            //    moveToMenuItem.MenuItems.Add("Center Tab", HandleMoveToCenterTab);
            //}
        }

        private void HandleMoveToLeftTab(object sender, EventArgs e)
        {
            //ParentTabControl.TabPages.Remove(this);
            //FlatRedBall.Glue.Plugins.PluginManager.LeftTab.TabPages.Add(this);
        }

        private void HandleMoveToRightTab(object sender, EventArgs e)
        {
            //ParentTabControl.TabPages.Remove(this);
            //FlatRedBall.Glue.Plugins.PluginManager.RightTab.TabPages.Add(this);

        }

        private void HandleMoveToTopTab(object sender, EventArgs e)
        {
            //ParentTabControl.TabPages.Remove(this);
            //FlatRedBall.Glue.Plugins.PluginManager.TopTab.TabPages.Add(this);

        }

        private void HandleMoveToBottomTab(object sender, EventArgs e)
        {
            //ParentTabControl.TabPages.Remove(this);
            //FlatRedBall.Glue.Plugins.PluginManager.BottomTab.TabPages.Add(this);

        }

        private void HandleMoveToCenterTab(object sender, EventArgs e)
        {
            //ParentTabControl.TabPages.Remove(this);
            //FlatRedBall.Glue.Plugins.PluginManager.CenterTab.TabPages.Add(this);

        }

        public void CloseTabByUser()
        {
            ClosedByUser?.Invoke(this);
        }
    }
}
