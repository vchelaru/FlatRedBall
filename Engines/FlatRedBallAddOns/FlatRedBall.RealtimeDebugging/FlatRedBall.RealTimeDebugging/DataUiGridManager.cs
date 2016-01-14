using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using WpfDataUi;

namespace FlatRedBall.RealTimeDebugging
{
    public class DataUiGridManager
    {
        DataUiGrid mDataUiGrid;
        TreeView mTreeView;

        public void Initialize(DataUiGrid dataUiGrid, TreeView treeView)
        {
            mDataUiGrid = dataUiGrid;
            mTreeView = treeView;

            FillTreeView();
        }

        private void FillTreeView()
        {
            var flatRedBall = AddToItem(null, "FlatRedBall");
            {
                var gui = AddToItem(flatRedBall, "Gui");
                {
                    var cursor = AddToItem(gui, "Cursor", "FlatRedBall.Gui.GuiManager.Cursor");
                }
            }
        }

        TreeViewItem AddToItem(TreeViewItem parent, string text, object tag = null)
        {
            TreeViewItem item = new TreeViewItem();
            item.Header = text;
            item.Tag = tag;
            if (parent != null)
            {
                parent.Items.Add(item);
            }
            else
            {
                mTreeView.Items.Add(item);
            }

            if (tag != null)
            {
                item.Selected += HandleTreeViewItemSelected;
            }
            return item;
        }

        private void HandleTreeViewItemSelected(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = sender as TreeViewItem;

            if (item != null && item.Tag != null)
            {
                string tagAsString = item.Tag as string;

                switch (tagAsString)
                {
                    case "FlatRedBall.Gui.GuiManager.Cursor":

                        HandleShowCursor();
                        break;
                }
                
            }
        }

        private void HandleShowCursor()
        {
            FlatRedBall.Gui.Cursor cursor = FlatRedBall.Gui.GuiManager.Cursor;

            mDataUiGrid.Instance = cursor;
            mDataUiGrid.MembersToIgnore.Add("si");
            mDataUiGrid.MembersToIgnore.Add("tipXOffset");
            mDataUiGrid.MembersToIgnore.Add("tipYOffset");
            mDataUiGrid.MembersToIgnore.Add("mWindowSecondaryPushed");
            mDataUiGrid.MembersToIgnore.Add("StaticPosition");
            mDataUiGrid.MembersToIgnore.Add("mWindowGrabbed");
            mDataUiGrid.MembersToIgnore.Add("ObjectGrabbedRelativeX");
            mDataUiGrid.MembersToIgnore.Add("ObjectGrabbedRelativeY");


        }

        internal void RefreshUi()
        {
            mDataUiGrid.Refresh();
        }
    }
}
