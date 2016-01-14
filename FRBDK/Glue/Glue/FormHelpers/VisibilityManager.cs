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
            bool shouldShowCodeEditWindow;
            DetermineWhatShouldBeShown(out shouldShowPropertyGrid, out shouldShowCodePreviewWindow, out shouldShowCodeEditWindow);

            TabControl tabControl = MainGlueWindow.Self.MainTabControl;
            
            if (shouldShowPropertyGrid)
            {
                AddTabToMain(MainGlueWindow.Self.PropertiesTab);              
            }
            else
            {
                RemoveTabFromMain(MainGlueWindow.Self.PropertiesTab);
            }

            if (shouldShowCodeEditWindow)
            {
                AddTabToMain(MainGlueWindow.Self.CodeTab);  

                MainGlueWindow.Self.CodeEditor.Visible = true;
                MainGlueWindow.Self.CodeEditor.UpdateDisplayToCurrentObject();

                SetUpCodeEditorForEventResponse();
            }
            else
            {
                MainGlueWindow.Self.CodeEditor.Visible = false;

                // The auto-complete list could still be visible
                // and we need to hide it so it doesn't show on top
                // of other UI.  I don't know why setting visible doesn't
                // raise the event for visibility changed, so we'll just have
                // to hide it manually.
                MainGlueWindow.Self.CodeEditor.HideAutoComplete();
            }

            if (shouldShowCodePreviewWindow)
            {
                MainGlueWindow.Self.CodePreviewTextBox.Visible = true;
                AddTabToMain(MainGlueWindow.Self.CodeTab);  
            }
            else
            {
                MainGlueWindow.Self.CodePreviewTextBox.Visible = false;
            }

            if (!shouldShowCodePreviewWindow && !shouldShowCodeEditWindow)
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

        private static void DetermineWhatShouldBeShown(out bool shouldShowPropertyGrid, out bool shouldShowCodePreviewWindow, out bool shouldShowCodeEditWindow)
        {




            if (EditorLogic.CurrentTreeNode != null && EditorLogic.CurrentTreeNode.IsEventResponseTreeNode())
            {
                shouldShowPropertyGrid = true;
                shouldShowCodePreviewWindow = false;
                shouldShowCodeEditWindow = true;

            }
            else if (EditorLogic.CurrentTreeNode != null && ShouldShowCode)
            {
                shouldShowPropertyGrid = true;
                shouldShowCodePreviewWindow = true;
                shouldShowCodeEditWindow = false;
            }
            else
            {
                shouldShowPropertyGrid = true;
                shouldShowCodeEditWindow = false;
                shouldShowCodePreviewWindow = false;
            }
        }

        private static void SetUpCodeEditorForEventResponse()
        {
            EventResponseSave ers = EditorLogic.CurrentEventResponseSave;

            EventSave eventSave = ers.GetEventSave();
            string args = ers.GetArgsForMethod(EditorLogic.CurrentElement);

            MainGlueWindow.Self.CodeEditor.TopText = "void On" + ers.EventName + "(" + args + ")\n{";
            MainGlueWindow.Self.CodeEditor.BottomText = "}";
        }
    }
}
