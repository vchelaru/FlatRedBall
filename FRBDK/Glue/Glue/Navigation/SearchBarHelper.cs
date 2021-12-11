using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Glue;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.FormHelpers;
using System.Windows.Forms;
using FlatRedBall.Glue.Events;
using System.Runtime.InteropServices;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GlueFormsCore.Plugins.EmbeddedPlugins.ExplorerTabPlugin;
using System.Windows.Input;

namespace FlatRedBall.Glue.Navigation
{
    public class SearchBarHelper
    {
        #region Fields

        private const int EM_SETCUEBANNER = 0x1501;
        private const int EM_GETCUEBANNER = 0x1502;
        private static TreeView TreeView;

        #endregion

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SendMessage(IntPtr hWnd, int msg,
        int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);


        public static void SearchBarTextChange(string text)
        {

            if (string.IsNullOrEmpty(text))
            {
                MainExplorerPlugin.Self.SearchListBox.Visibility = System.Windows.Visibility.Collapsed;
                MainExplorerPlugin.Self.ElementTreeViewContainer.Visibility = System.Windows.Visibility.Visible;

            }
            else
            {
                MainExplorerPlugin.Self.SearchListBox.Visibility = System.Windows.Visibility.Visible;
                //MainExplorerPlugin.Self.ElementTreeView.Visible = false;
                MainExplorerPlugin.Self.ElementTreeViewContainer.Visibility = System.Windows.Visibility.Collapsed;

                FillSearchBoxWithOptions(text);
                if (MainExplorerPlugin.Self.SearchListBox.Items.Count > 0)
                {
                    MainExplorerPlugin.Self.SearchListBox.SelectedIndex = 0;
                }
            }


        }

        static void SetCueText(Control control, string text)
        {
            SendMessage(control.Handle, EM_SETCUEBANNER, 0, text);
        }

        static void FillSearchBoxWithOptions(string text)
        {
            GlueProjectSave glueProjectSave = ProjectManager.GlueProjectSave;

            if (glueProjectSave == null)
            {
                return;
            }

            // Order it like this:
            // 1:  Screens
            // 2:  Entities
            // 3:  Objects (instances)
            // 4:  Files
            // 5:  States
            // 6:  State Categories
            // 7:  Variables

            text = text.ToLower();

            MainExplorerPlugin.Self.SearchListBox.Items.Clear();


            bool showScreens;
            bool showEntities;
            bool showObjects;
            bool showFiles;
            bool showStates;
            bool showVariables;
            bool showEvents;
            bool showCategories;
            text = DetermineWhatIsShown(text, 
                out showScreens, out showEntities, 
                out showObjects, out showFiles, 
                out showStates, out showVariables,
                out showEvents, out showCategories);

            List<object> toAdd = new List<object>();


            #region Add Screens
            if (showScreens)
            {
                List<object> tempList = new List<object>();

                foreach (ScreenSave screen in glueProjectSave.Screens)
                {
                    string name = screen.Name.ToLower();
                    name = name.Substring("screens\\".Length);

                    if (name.Contains(text))
                    {
                        tempList.Add(screen);
                    }
                }

                tempList = tempList.OrderBy(item => item.ToString()).ToList();
                toAdd.AddRange(tempList);

            }
            #endregion

            #region Add Entities
            if (showEntities)
            {
                List<object> tempList = new List<object>();

                foreach (EntitySave entity in glueProjectSave.Entities)
                {
                    string name = entity.Name.ToLower();
                    name = name.Substring("entities\\".Length);
                    if (name.Contains(text))
                    {
                        tempList.Add(entity);

                    }
                }

                tempList = tempList.OrderBy(item => item.ToString()).ToList();
                toAdd.AddRange(tempList);
            }
            #endregion

            #region Add Objects (instances)
            if (showObjects)
            {
                List<object> tempList = new List<object>();

                foreach (ScreenSave screen in glueProjectSave.Screens)
                {
                    List<NamedObjectSave> namedObjectList = screen.NamedObjects;

                    FillSearchListBoxWithMatchingNos(text, namedObjectList, tempList);
                }

                foreach (EntitySave entity in glueProjectSave.Entities)
                {
                    List<NamedObjectSave> namedObjectList = entity.NamedObjects;

                    FillSearchListBoxWithMatchingNos(text, namedObjectList, tempList);
                }
                tempList = tempList.OrderBy(item => item.ToString()).ToList();
                toAdd.AddRange(tempList);

            }

            #endregion

            #region Add Files

            if (showFiles)
            {
                List<object> tempList = new List<object>();

                foreach (ScreenSave screen in glueProjectSave.Screens)
                {
                    foreach (ReferencedFileSave rfs in screen.ReferencedFiles)
                    {
                        if (rfs.Name.ToLower().Contains(text))
                        {
                            tempList.Add(rfs);
                        }
                    }
                }

                foreach (EntitySave entity in glueProjectSave.Entities)
                {
                    foreach (ReferencedFileSave rfs in entity.ReferencedFiles)
                    {
                        if (rfs.Name.ToLower().Contains(text))
                        {
                            tempList.Add(rfs);
                        }
                    }
                }

                foreach (ReferencedFileSave rfs in glueProjectSave.GlobalFiles)
                {
                    if (rfs.Name.ToLower().Contains(text))
                    {
                        tempList.Add(rfs);
                    }
                }
                tempList = tempList.OrderBy(item => item.ToString()).ToList();
                toAdd.AddRange(tempList);

            }


            #endregion

            #region Add States

            if (showStates)
            {
                List<object> tempList = new List<object>();

                foreach (ScreenSave screen in glueProjectSave.Screens)
                {
                    foreach (StateSave state in screen.AllStates)
                    {
                        if (state.Name.ToLower().Contains(text))
                        {
                            tempList.Add(state);
                        }
                    }
                }

                foreach (EntitySave entity in glueProjectSave.Entities)
                {
                    foreach (StateSave state in entity.AllStates)
                    {
                        if (state.Name.ToLower().Contains(text))
                        {
                            tempList.Add(state);
                        }
                    }
                }
                tempList = tempList.OrderBy(item => item.ToString()).ToList();
                toAdd.AddRange(tempList);

            }

            #endregion

            #region Add State Categories

            if (showCategories)
            {
                List<object> tempList = new List<object>();

                foreach (ScreenSave screen in glueProjectSave.Screens)
                {
                    foreach (StateSaveCategory category in screen.StateCategoryList)
                    {
                        if (category.Name.ToLower().Contains(text))
                        {
                            tempList.Add(category);
                        }
                    }
                }

                foreach (EntitySave entity in glueProjectSave.Entities)
                {
                    foreach (StateSaveCategory category in entity.StateCategoryList)
                    {
                        if (category.Name.ToLower().Contains(text))
                        {
                            tempList.Add(category);
                        }
                    }
                }
                tempList = tempList.OrderBy(item => item.ToString()).ToList();
                toAdd.AddRange(tempList);

            }

            #endregion

            #region Add Variables

            if (showVariables)
            {
                List<object> tempList = new List<object>();

                foreach (ScreenSave screen in glueProjectSave.Screens)
                {
                    foreach (CustomVariable variable in screen.CustomVariables)
                    {
                        if (variable.Name.ToLower().Contains(text))
                        {
                            tempList.Add(variable);
                        }
                    }
                }

                foreach (EntitySave entity in glueProjectSave.Entities)
                {
                    foreach (CustomVariable variable in entity.CustomVariables)
                    {
                        if (variable.Name.ToLower().Contains(text))
                        {
                            tempList.Add(variable);
                        }
                    }
                }
                tempList = tempList.OrderBy(item => item.ToString()).ToList();
                toAdd.AddRange(tempList);

            }

            #endregion

            #region Add Events

            if (showEvents)
            {
                List<object> tempList = new List<object>();

                foreach (ScreenSave screen in glueProjectSave.Screens)
                {
                    foreach (EventResponseSave eventResponse in screen.Events)
                    {
                        if (eventResponse.EventName.ToLower().Contains(text))
                        {
                            tempList.Add(eventResponse);
                        }
                    }
                }

                foreach (EntitySave entity in glueProjectSave.Entities)
                {
                    foreach (EventResponseSave eventResponse in entity.Events)
                    {
                        if (eventResponse.EventName.ToLower().Contains(text))
                        {
                            tempList.Add(eventResponse);
                        }
                    }
                }
                tempList = tempList.OrderBy(item => item.ToString()).ToList();
                toAdd.AddRange(tempList);

            }
            #endregion

            foreach(var item in toAdd.ToArray())
            {
                MainExplorerPlugin.Self.SearchListBox.Items.Add(item);

            }
        }

        public static void Initialize(System.Windows.Forms.TreeView treeView)
        {
            TreeView = treeView;
        }

        private static string DetermineWhatIsShown(string text, out bool showScreens, out bool showEntities, out bool showObjects, out bool showFiles, out bool showStates, out bool showVariables, out bool showEvents, out bool showCategories)
        {
            showScreens = true;
            showEntities = true;
            showObjects = true;
            showFiles = true;
            showStates = true;
            showVariables = true;
            showEvents = true;
            showCategories = true;

            if (text.StartsWith("s "))
            {
                showEntities = false;
                showObjects = false;
                showVariables = false;
                showFiles = false;
                showStates = false;
                showEvents = false;
                showCategories = false;

                text = text.Substring(2);
            }
            else if (text.StartsWith("e "))
            {
                showScreens = false;
                showObjects = false;
                showVariables = false;
                showFiles = false;
                showStates = false;

                showStates = false;
                showEvents = false;
                showCategories = false;

                text = text.Substring(2);
            }
            else if (text.StartsWith("o "))
            {
                showScreens = false;
                showEntities = false;
                showVariables = false;
                showFiles = false;

                showStates = false;
                showEvents = false;
                showCategories = false;

                text = text.Substring(2);
            }
            else if (text.StartsWith("v "))
            {
                showScreens = false;
                showEntities = false;
                showObjects = false;
                showFiles = false;

                showStates = false;
                showEvents = false;
                showCategories = false;

                text = text.Substring(2);
            }
            else if (text.StartsWith("f "))
            {
                showScreens = false;
                showEntities = false;
                showObjects = false;
                showVariables = false;

                showStates = false;
                showEvents = false;
                showCategories = false;

                text = text.Substring(2);
            }
            else if (text.StartsWith("ev "))
            {
                showScreens = false;
                showEntities = false;
                showObjects = false;
                showVariables = false;
                showStates = false;
                showFiles = false;
                showEvents = true;
                showCategories = false;


                text = text.Substring(3);
            }
            else if (text.StartsWith("c "))
            {
                showScreens = false;
                showEntities = false;
                showObjects = false;
                showVariables = false;
                showStates = false;
                showFiles = false;
                showEvents = false;
                showCategories = true;


                text = text.Substring(2);
            }
            return text;
        }

        private static void FillSearchListBoxWithMatchingNos(string text, List<NamedObjectSave> namedObjectList, List<object> toAdd)
        {
            foreach (NamedObjectSave nos in namedObjectList)
            {
                if (nos.InstanceName.ToLower().Contains(text))
                {
                    toAdd.Add(nos);
                }

                FillSearchListBoxWithMatchingNos(text, nos.ContainedObjects, toAdd);
            }
        }

        public static void SearchListBoxIndexChanged()
        {

            Object selectedObject = MainExplorerPlugin.Self.SearchListBox.SelectedItem;

            bool foundSomething = false;
            if (MainExplorerPlugin.Self.SearchListBox.Visibility == System.Windows.Visibility.Visible)
            {
                if (selectedObject is ScreenSave screen)
                {
                    GlueState.Self.CurrentScreenSave = screen;
                    foundSomething = true;
                }
                else if (selectedObject is EntitySave entity)
                {
                    GlueState.Self.CurrentEntitySave = entity;
                    foundSomething = true;
                }
                else if (selectedObject is NamedObjectSave namedObject)
                {
                    GlueState.Self.CurrentNamedObjectSave = namedObject;
                    foundSomething = true;
                }
                else if (selectedObject is ReferencedFileSave referencedFile)
                {
                    GlueState.Self.CurrentReferencedFileSave = referencedFile;
                    foundSomething = true;
                }
                else if (selectedObject is StateSave stateSave)
                {
                    GlueState.Self.CurrentStateSave = stateSave;                    
                    foundSomething = true;
                }
                else if (selectedObject is CustomVariable customVariable)
                {
                    GlueState.Self.CurrentCustomVariable = customVariable;
                    foundSomething = true;
                }
                else if (selectedObject is EventResponseSave eventResponse)
                {
                    GlueState.Self.CurrentEventResponseSave = eventResponse;
                    foundSomething = true;
                }
                else if (selectedObject is StateSaveCategory stateSaveCategory)
                {
                    GlueState.Self.CurrentStateSaveCategory = stateSaveCategory;
                    foundSomething = true;
                }
            }
            if (foundSomething)
            {
                ElementViewWindow.SelectedNodeOld.Expand();

                // Sometimes the selected object isn't selected
                // This line seems to solve it.
                ElementViewWindow.SelectedNodeOld = ElementViewWindow.SelectedNodeOld;
            }

            MainExplorerPlugin.Self.SearchListBox.Visibility = System.Windows.Visibility.Collapsed;
            //MainExplorerPlugin.Self.ElementTreeView.Visible = true;
            MainExplorerPlugin.Self.ElementTreeViewContainer.Visibility = System.Windows.Visibility.Visible;

            MainExplorerPlugin.Self.SearchTextbox.Text = null;


        }

        internal static void TextBoxKeyDown(System.Windows.Input.KeyEventArgs args)
        {
            if (args.Key == System.Windows.Input.Key.Escape)
            {
                args.Handled = true;
                LoseTextBoxFocus();

                //TextBoxLeave(MainExplorerPlugin.Self.SearchTextbox);
            }
            else if (args.Key == System.Windows.Input.Key.Up)
            {
                if (MainExplorerPlugin.Self.SearchListBox.SelectedIndex > 0)
                {
                    args.Handled = true;
                    MainExplorerPlugin.Self.SearchListBox.SelectedIndex--;
                }
            }
            else if (args.Key == System.Windows.Input.Key.Down)
            {
                if (MainExplorerPlugin.Self.SearchListBox.SelectedIndex < MainExplorerPlugin.Self.SearchListBox.Items.Count - 1)
                {
                    args.Handled = true;
                    MainExplorerPlugin.Self.SearchListBox.SelectedIndex++;
                }
            }
            else if (args.Key == System.Windows.Input.Key.Enter)
            {
                args.Handled = true;
                SearchListBoxIndexChanged();
                //TextBoxLeave(MainExplorerPlugin.Self.SearchTextbox);
                LoseTextBoxFocus();
            }

            void LoseTextBoxFocus()
            {
                // Apr 2, 2021
                // It seems like this
                // only returns focus to 
                // the treeview if the text
                // box is empty when this is 
                // called. I don't know why, I
                // suspect it has to do with the 
                // treeview still being winforms.
                // I did burn quite a bit of time investigating
                // and decided to not waste more time and instead
                // log my findings. Eventually this will get revisited
                // when the tree view becomes wpf.

                Keyboard.ClearFocus();
                MainExplorerPlugin.Self.SearchTextbox.Text = null;

                MainExplorerPlugin.Self.SearchListBox.Visibility = System.Windows.Visibility.Collapsed;
                MainExplorerPlugin.Self.ElementTreeViewContainer.Visibility = System.Windows.Visibility.Visible;

                Keyboard.Focus(MainExplorerPlugin.Self.ElementTreeViewContainer);

                MainExplorerPlugin.Self.ElementTreeViewContainer.Focus();
                TreeView.Focus();
            }
        }

        internal static void TextBoxKeyDown(System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                MainExplorerPlugin.Self.SearchTextbox.Text = null;

                MainExplorerPlugin.Self.SearchListBox.Visibility = System.Windows.Visibility.Collapsed;
                //MainExplorerPlugin.Self.ElementTreeView.Visible = true;
                MainExplorerPlugin.Self.ElementTreeViewContainer.Visibility = System.Windows.Visibility.Visible;

                //TextBoxLeave(MainExplorerPlugin.Self.SearchTextbox);
            }
            else if (e.KeyCode == Keys.Up)
            {
                if (MainExplorerPlugin.Self.SearchListBox.SelectedIndex > 0)
                {
                    MainExplorerPlugin.Self.SearchListBox.SelectedIndex--;
                }
            }
            else if (e.KeyCode == Keys.Down)
            {
                if (MainExplorerPlugin.Self.SearchListBox.SelectedIndex < MainExplorerPlugin.Self.SearchListBox.Items.Count - 1)
                {
                    MainExplorerPlugin.Self.SearchListBox.SelectedIndex++;
                }
            }
            else if (e.KeyCode == Keys.Enter)
            {
                SearchListBoxIndexChanged();
                e.Handled = true;
                e.SuppressKeyPress = true;

                TreeView.Focus();
                //TextBoxLeave(MainExplorerPlugin.Self.SearchTextbox);

            }
        }
    }
}
