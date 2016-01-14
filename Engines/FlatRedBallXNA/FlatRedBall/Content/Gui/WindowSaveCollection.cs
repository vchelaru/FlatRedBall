using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using FlatRedBall.Gui;
using System.Reflection;
using FlatRedBall.IO;
using FlatRedBall.Utilities;

namespace FlatRedBall.Content.Gui
{
    public class WindowSaveCollection
    {
        #region Fields

        [XmlElementAttribute("WindowSave")]
        public List<WindowSave> WindowSaves = new List<WindowSave>();

        #endregion

        #region Methods

        public void ApplyTo(IList<IWindow> windows, string contentManager)
        {

            foreach (Window window in windows)
            {
                WindowSave ws = this.FindByName(window.Name);

                if (ws == null)
                {
                    continue;
                }

                Type windowType = window.GetType();

                if (windowType == typeof(Button))
                {
                    ((ButtonSave)ws).SetRuntime((Button)window, contentManager);
                }
                else if (windowType == typeof(CollapseListBox))
                {
                    ((CollapseListBoxSave)ws).SetRuntime((CollapseListBox)window, contentManager);
                }
                else if (window is CollapseWindow)
                {
                    ((CollapseWindowSave)ws).SetRuntime((CollapseWindow)window, contentManager);
                }
                else if (windowType == typeof(ColorDisplay))
                {
                    ((ColorDisplaySave)ws).SetRuntime((ColorDisplay)window, contentManager);
                }
                else if (windowType == typeof(ComboBox))
                {
                    ((ComboBoxSave)ws).SetRuntime((ComboBox)window, contentManager);
                }
                else if (windowType == typeof(ListBoxBase))
                {
                    ((ListBoxBaseSave)ws).SetRuntime((ListBoxBase)window, contentManager);
                }
                else if (windowType == typeof(ListBox))
                {
                    ((ListBoxSave)ws).SetRuntime((ListBox)window, contentManager);
                }
                else if (windowType == typeof(MarkerTimeLine))
                {
                    ((MarkerTimeLineSave)ws).SetRuntime((MarkerTimeLine)window, contentManager);
                }
                else if (windowType == typeof(ScrollBar))
                {
                    ((ScrollBarSave)ws).SetRuntime((ScrollBar)window, contentManager);
                }
                else if (windowType == typeof(TextBox))
                {
                    ((TextBoxSave)ws).SetRuntime((TextBox)window, contentManager);
                }
                else if (windowType == typeof(TextDisplay))
                {
                    ((TextDisplaySave)ws).SetRuntime((TextDisplay)window, contentManager);
                }
                else if (windowType == typeof(TimeLine))
                {
                    ((TimeLineSave)ws).SetRuntime((TimeLine)window, contentManager);
                }
                else if (windowType == typeof(ToggleButton))
                {
                    ((ToggleButtonSave)ws).SetRuntime((ToggleButton)window, contentManager);
                }
                else if (windowType == typeof(UpDown))
                {
                    ((UpDownSave)ws).SetRuntime((UpDown)window, contentManager);
                }
                else if (windowType == typeof(Vector3Display))
                {
                    ((Vector3DisplaySave)ws).SetRuntime((Vector3Display)window, contentManager);
                }
                else // treat everything else as a Window.
                {
                    ws.SetRuntime(window, contentManager);
                }

            }
        }

        public WindowSave FindByName(string nameToFind)
        {
            for (int i = 0; i < this.WindowSaves.Count; i++)
            {
                if (WindowSaves[i].Name == nameToFind)
                {
                    return WindowSaves[i];
                }
            }
            return null;
        }

        public static WindowSaveCollection FromRuntime(IList<IWindow> windows)
        {
            WindowSaveCollection wsc = new WindowSaveCollection();

            // Not sure why these are here but they cause warnings
            // so removing them.
            //string saveClassNamespace = "FlatRedBall.Content.Gui";
            //string runtimeClassNamespace = "FlatRedBall.Gui.";
            Type[] saveTypes = Assembly.GetExecutingAssembly().GetTypes();

            Dictionary<Type, MethodInfo> mFromRuntimeMethods = new Dictionary<Type,MethodInfo>();

            foreach (Window window in windows)
            {
                Type windowType = window.GetType();

                Type[] types = windowType.GetInterfaces();

                if (windowType == typeof(Button))
                {
                    wsc.WindowSaves.Add(ButtonSave.FromRuntime<ButtonSave>(window as Button));
                }
                else if (windowType == typeof(CollapseListBox))
                {
                    wsc.WindowSaves.Add(CollapseListBoxSave.FromRuntime<CollapseListBoxSave>(window as CollapseListBox));
                }
                else if (window is CollapseWindow)
                {
                    wsc.WindowSaves.Add(CollapseWindowSave.FromRuntime<CollapseWindowSave>(window as CollapseWindow));

                }
                else if (windowType == typeof(ColorDisplay))
                {
                    wsc.WindowSaves.Add(ColorDisplaySave.FromRuntime<ColorDisplaySave>(window as ColorDisplay));
                }
                else if (windowType == typeof(ComboBox))
                {
                    wsc.WindowSaves.Add(ComboBoxSave.FromRuntime<ComboBoxSave>(window as ComboBox));
                }
                else if (windowType == typeof(ListBoxBase))
                {
                    wsc.WindowSaves.Add(ListBoxBaseSave.FromRuntime<ListBoxBaseSave>(window as ListBoxBase));
                }
                else if (windowType == typeof(ListBox))
                {
                    wsc.WindowSaves.Add(ListBoxSave.FromRuntime<ListBoxSave>(window as ListBox));
                }
                else if (windowType == typeof(MarkerTimeLine))
                {
                    wsc.WindowSaves.Add(MarkerTimeLineSave.FromRuntime<MarkerTimeLineSave>(window as MarkerTimeLine));
                }
                else if (windowType == typeof(ScrollBar))
                {
                    wsc.WindowSaves.Add(ScrollBarSave.FromRuntime<ScrollBarSave>(window as ScrollBar));
                }
                else if (windowType == typeof(TextBox))
                {
                    wsc.WindowSaves.Add(TextBoxSave.FromRuntime<TextBoxSave>(window as TextBox));
                }
                else if (windowType == typeof(TextDisplay))
                {
                    wsc.WindowSaves.Add(TextDisplaySave.FromRuntime<TextDisplaySave>(window as TextDisplay));
                }
                else if (windowType == typeof(TimeLine))
                {
                    wsc.WindowSaves.Add(TimeLineSave.FromRuntime<TimeLineSave>(window as TimeLine));
                }
                else if (windowType == typeof(ToggleButton))
                {
                    wsc.WindowSaves.Add(ToggleButtonSave.FromRuntime<ToggleButtonSave>(window as ToggleButton));
                }
                else if (windowType == typeof(UpDown))
                {
                    wsc.WindowSaves.Add(UpDownSave.FromRuntime<UpDownSave>(window as UpDown));
                }
                else if (windowType == typeof(Vector3Display))
                {
                    wsc.WindowSaves.Add(Vector3DisplaySave.FromRuntime<Vector3DisplaySave>(window as Vector3Display));
                }
                else // treat everything else as a Window.
                {
                    wsc.WindowSaves.Add(WindowSave.FromRuntime<WindowSave>(window as Window));
                }
                
            }

            return wsc;
        }

        public static WindowSaveCollection FromFile(string fileName)
        {
            return FileManager.XmlDeserialize<WindowSaveCollection>(fileName);
        }

        public void RemoveWindow(string windowName)
        {
            for (int i = WindowSaves.Count - 1; i > -1; i--)
            {
                if (windowName == WindowSaves[i].Name)
                {
                    WindowSaves.RemoveAt(i);
                    break;
                }
            }

        }

        public void Save(string fileName)
        {
            FileManager.XmlSerialize(this, fileName);
        }

        public WindowArray ToRuntime(string contentManagerName)
        {
            WindowArray listToReturn = new WindowArray();

            #region Instantiate the Windows

            foreach (WindowSave windowSave in this.WindowSaves)
            {
                Type windowType = windowSave.GetType();

                if (windowType == typeof(ButtonSave))
                {
                    listToReturn.Add(((ButtonSave)windowSave).ToRuntime(contentManagerName, GuiManager.Cursor));
                }
                else if (windowType == typeof(CollapseListBoxSave))
                {
                    listToReturn.Add(((CollapseListBoxSave)windowSave).ToRuntime(contentManagerName, GuiManager.Cursor));
                }
                else if (windowSave is CollapseWindowSave)
                {
                    listToReturn.Add(((CollapseWindowSave)windowSave).ToRuntime(contentManagerName, GuiManager.Cursor));
                }
                else if (windowType == typeof(ColorDisplaySave))
                {
                    listToReturn.Add(((ColorDisplaySave)windowSave).ToRuntime(contentManagerName, GuiManager.Cursor));
                }
                else if (windowType == typeof(ComboBoxSave))
                {
                    listToReturn.Add(((ComboBoxSave)windowSave).ToRuntime(contentManagerName, GuiManager.Cursor));
                }
                else if (windowType == typeof(ListBoxBaseSave))
                {
                    listToReturn.Add(((ListBoxBaseSave)windowSave).ToRuntime(contentManagerName, GuiManager.Cursor));
                }
                else if (windowType == typeof(ListBoxSave))
                {
                    listToReturn.Add(((ListBoxSave)windowSave).ToRuntime(contentManagerName, GuiManager.Cursor));
                }
                else if (windowType == typeof(MarkerTimeLineSave))
                {
                    listToReturn.Add(((MarkerTimeLineSave)windowSave).ToRuntime(contentManagerName, GuiManager.Cursor));
                }
                else if (windowType == typeof(ScrollBarSave))
                {
                    listToReturn.Add(((ScrollBarSave)windowSave).ToRuntime(contentManagerName, GuiManager.Cursor));
                }
                else if (windowType == typeof(TextBoxSave))
                {
                    listToReturn.Add(((TextBoxSave)windowSave).ToRuntime(contentManagerName, GuiManager.Cursor));
                }
                else if (windowType == typeof(TextDisplaySave))
                {
                    listToReturn.Add(((TextDisplaySave)windowSave).ToRuntime(contentManagerName, GuiManager.Cursor));
                }
                else if (windowType == typeof(TimeLineSave))
                {
                    listToReturn.Add(((TimeLineSave)windowSave).ToRuntime(contentManagerName, GuiManager.Cursor));
                }
                else if (windowType == typeof(ToggleButtonSave))
                {
                    listToReturn.Add(((ToggleButtonSave)windowSave).ToRuntime(contentManagerName, GuiManager.Cursor));
                }
                else if (windowType == typeof(UpDownSave))
                {
                    listToReturn.Add(((UpDownSave)windowSave).ToRuntime(contentManagerName, GuiManager.Cursor));
                }
                else if (windowType == typeof(Vector3DisplaySave))
                {
                    listToReturn.Add(((Vector3DisplaySave)windowSave).ToRuntime(contentManagerName, GuiManager.Cursor));
                }
                else if (windowType == typeof(WindowSave))
                {
                    listToReturn.Add(((WindowSave)windowSave).ToRuntime(contentManagerName, GuiManager.Cursor));
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            #endregion

            for (int i = 0; i < WindowSaves.Count; i++)
            {
                if (!string.IsNullOrEmpty(WindowSaves[i].Parent))
                {
                    Window window = listToReturn.FindByName(WindowSaves[i].Parent) as Window;

                    window.AddWindow(listToReturn[i]);
                }
            }

            return listToReturn;
        }

        #endregion
    }
}
