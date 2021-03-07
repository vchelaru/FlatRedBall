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

namespace FlatRedBall.Glue.Navigation
{
    public class SearchBarHelper
    {
        #region Fields

        private const int EM_SETCUEBANNER = 0x1501;
        private const int EM_GETCUEBANNER = 0x1502;

        #endregion

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SendMessage(IntPtr hWnd, int msg,
        int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        public static void Initialize(System.Windows.Forms.TextBox textBox, string text = "Search... (CTRL + F)")
        {
            SetCueText(textBox, text);
        }



        public static void SearchBarTextChange()
        {
            string text = MainExplorerPlugin.Self.SearchTextbox.Text;

            if (string.IsNullOrEmpty(text))
            {
                MainExplorerPlugin.Self.SearchListBox.Visible = false;

            }
            else
            {
                MainExplorerPlugin.Self.SearchListBox.Visible = true;
                FillSearchBoxWithOptions(text);
                if (MainExplorerPlugin.Self.SearchListBox.Items.Count > 0)
                {
                    MainExplorerPlugin.Self.SearchListBox.SelectedIndex = 0;
                }
            }

            MainExplorerPlugin.Self.ElementTreeView.Visible = !MainExplorerPlugin.Self.SearchListBox.Visible;

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


            MainExplorerPlugin.Self.SearchListBox.Items.AddRange(toAdd.ToArray());
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
            if (MainExplorerPlugin.Self.SearchListBox.Visible)
            {
                if (selectedObject is ScreenSave)
                {
                    ElementViewWindow.SelectedNode = GlueState.Self.Find.ScreenTreeNode((ScreenSave)selectedObject);
                    foundSomething = ElementViewWindow.SelectedNode != null;
                }
                else if (selectedObject is EntitySave)
                {
                    ElementViewWindow.SelectedNode = GlueState.Self.Find.EntityTreeNode((EntitySave)selectedObject);
                    foundSomething = ElementViewWindow.SelectedNode != null;
                }
                else if (selectedObject is NamedObjectSave)
                {
                    ElementViewWindow.SelectedNode = GlueState.Self.Find.NamedObjectTreeNode((NamedObjectSave)selectedObject);
                    foundSomething = ElementViewWindow.SelectedNode != null;
                }
                else if (selectedObject is ReferencedFileSave)
                {
                    ElementViewWindow.SelectedNode = GlueState.Self.Find.ReferencedFileSaveTreeNode((ReferencedFileSave)selectedObject);
                    foundSomething = ElementViewWindow.SelectedNode != null;
                }
                else if (selectedObject is StateSave)
                {
                    ElementViewWindow.SelectedNode = GlueState.Self.Find.StateTreeNode((StateSave)selectedObject);
                    foundSomething = ElementViewWindow.SelectedNode != null;
                }
                else if (selectedObject is CustomVariable)
                {
                    ElementViewWindow.SelectedNode = GlueState.Self.Find.CustomVariableTreeNode((CustomVariable)selectedObject);
                    foundSomething = ElementViewWindow.SelectedNode != null;
                }
                else if (selectedObject is EventResponseSave)
                {
                    ElementViewWindow.SelectedNode = GlueState.Self.Find.EventResponseTreeNode((EventResponseSave)selectedObject);
                    foundSomething = ElementViewWindow.SelectedNode != null;
                }
                else if (selectedObject is StateSaveCategory)
                {
                    ElementViewWindow.SelectedNode = GlueState.Self.Find.StateCategoryTreeNode((StateSaveCategory)selectedObject);

                    foundSomething = ElementViewWindow.SelectedNode != null;
                }
            }
            if (foundSomething)
            {
                ElementViewWindow.SelectedNode.Expand();

                // Sometimes the selected object isn't selected
                // This line seems to solve it.
                ElementViewWindow.SelectedNode = ElementViewWindow.SelectedNode;
            }

            MainExplorerPlugin.Self.SearchListBox.Visible = false;
            MainExplorerPlugin.Self.ElementTreeView.Visible = true;

            MainExplorerPlugin.Self.SearchTextbox.Text = null;


        }



        internal static void TextBoxKeyDown(System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                MainExplorerPlugin.Self.SearchTextbox.Text = null;

                MainExplorerPlugin.Self.SearchListBox.Visible = false;
                MainExplorerPlugin.Self.ElementTreeView.Visible = true;

                TextBoxLeave(MainExplorerPlugin.Self.SearchTextbox);
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

                MainExplorerPlugin.Self.ElementTreeView.Focus();
                TextBoxLeave(MainExplorerPlugin.Self.SearchTextbox);

            }
        }

        internal static void TextBoxLeave(TextBox textBox)
        {

        }
    }
}
