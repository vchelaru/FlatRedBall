using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Glue;
using FlatRedBall.Glue.Events;
using System.Windows.Forms;

namespace FlatRedBall.Glue.FormHelpers
{
    public class VisibilityManager
    {
        static bool ShouldShowCode
        {
            get
            {
                return !string.IsNullOrEmpty(MainGlueWindow.Self.CodePreviewTextBox.Text);

            }
        }

        public static void ReactivelySetItemViewVisibility()
        {
            bool shouldShowPropertyGrid;
            bool shouldShowCodePreviewWindow;
            DetermineWhatShouldBeShown(out shouldShowCodePreviewWindow);

            TabControl tabControl = MainGlueWindow.Self.MainTabControl;
            
            if (shouldShowCodePreviewWindow)
            {
                MainGlueWindow.Self.CodePreviewTextBox.Visible = true;
                AddTabToMain(MainGlueWindow.Self.CodeTab);  
            }
            else
            {
                MainGlueWindow.Self.CodePreviewTextBox.Visible = false;
            }

            if (!shouldShowCodePreviewWindow)
            {
                RemoveTabFromMain(MainGlueWindow.Self.CodeTab);
            }

        }

        private static void RemoveTabFromMain(TabPage tabPage)
        {
            TabControl tabControl = MainGlueWindow.Self.MainTabControl;
            if (tabControl.Controls.Contains(tabPage))
            {
                tabControl.Controls.Remove(tabPage);
            }
        }

        private static void AddTabToMain(TabPage tabPage)
        {
            TabControl tabControl = MainGlueWindow.Self.MainTabControl;
            if (!tabControl.Controls.Contains(tabPage))
            {
                tabControl.Controls.Add(tabPage);
            }

        }

        public static void DetermineWhatShouldBeShown(out bool shouldShowCodePreviewWindow)
        {




            if (EditorLogic.CurrentTreeNode != null && EditorLogic.CurrentTreeNode.IsEventResponseTreeNode())
            {
                shouldShowCodePreviewWindow = true;

            }
            else if (EditorLogic.CurrentTreeNode != null && ShouldShowCode)
            {
                shouldShowCodePreviewWindow = true;
            }
            else
            {
                shouldShowCodePreviewWindow = false;
            }
        }

    }
}
