using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Content.Gui;
using FlatRedBall.Gui;
using FlatRedBall.IO;
using System.IO;

namespace EditorObjects.Gui
{
    public static class LayoutManager
    {
        public static string WindowLayoutFileName
        {
            get
            {
                return FileWindow.ApplicationFolderForThisProgram + "windowLayout.wsc";
            }
        }

        public static void SaveWindowLayout()
        {
            WindowSaveCollection wsc = GuiManager.GetCurrentLayout();

            string fileName = WindowLayoutFileName;

            wsc.Save(fileName);

        }

        public static void LoadWindowLayout()
        {
            LoadWindowLayout(null);
        }

        public static void LoadWindowLayout(List<string> windowNamesToRemove)
        {
            try
            {
                string fileName = WindowLayoutFileName;
                if (FileManager.FileExists(fileName))
                {

                    WindowSaveCollection wsc = WindowSaveCollection.FromFile(fileName);

                    if (windowNamesToRemove != null)
                    {
                        foreach (string nameToRemove in windowNamesToRemove)
                        {
                            wsc.RemoveWindow(nameToRemove);
                        }
                    }
                    List<bool> moveswhenGrabbed = new List<bool>();
                    foreach (Window window in GuiManager.Windows)
                    {
                        moveswhenGrabbed.Add(window.MovesWhenGrabbed);
                    }
                    GuiManager.SetLayout(wsc);

                    for(int i = 0; i < moveswhenGrabbed.Count; i++)
                    {
                        GuiManager.Windows[i].MovesWhenGrabbed = moveswhenGrabbed[i];
                    }

                }
            }
            catch
            {
                // do nothing
            }
        }
    }
}
