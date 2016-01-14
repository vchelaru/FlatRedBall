using FlatRedBall.Glue.Managers;
using FlatRedBall.IO;
using Gum.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GumPlugin.Managers
{
    public class AppState : Singleton<AppState>
    {
        public GumProjectSave GumProjectSave
        {
            get { return Gum.Managers.ObjectFinder.Self.GumProjectSave; }
            set { Gum.Managers.ObjectFinder.Self.GumProjectSave = value; }
        }

        public IEnumerable<ElementSave> AllLoadedElements
        {
            get
            {
                if (GumProjectSave != null)
                {
                    foreach (var item in GumProjectSave.StandardElements)
                    {
                        yield return item;
                    }
                    foreach (var item in GumProjectSave.Components)
                    {
                        yield return item;
                    }
                    foreach (var item in GumProjectSave.Screens)
                    {
                        yield return item;
                    }
                }
            }
        }

        public string GlueProjectFolder 
        {
            get
            {
                return FileManager.GetDirectory(
                    FlatRedBall.Glue.Plugins.ExportedImplementations.GlueState.Self.CurrentGlueProjectFileName);
            }
        }


        public string GumProjectFolder
        {
            get
            {
                if (GumProjectSave == null)
                {
                    return null;
                }
                else
                {
                    return FileManager.GetDirectory(GumProjectSave.FullFileName);
                }
            }
        }

        public ElementSave GetElementSave(string elementName)
        {
            ScreenSave screenSave = GetScreen(elementName);
            if (screenSave != null)
            {
                return screenSave;
            }

            ComponentSave componentSave = GetComponent(elementName);
            if (componentSave != null)
            {
                return componentSave;
            }

            StandardElementSave standardElementSave = GetStandardElement(elementName);
            if (standardElementSave != null)
            {
                return standardElementSave;
            }

            // If we got here there's nothing by the argument name
            return null;

        }


        public ScreenSave GetScreen(string screenName)
        {
            GumProjectSave gps = GumProjectSave;

            if (gps != null)
            {
                foreach (ScreenSave screenSave in gps.Screens)
                {
                    if (screenSave.Name == screenName)
                    {
                        return screenSave;
                    }
                }

            }

            return null;
        }

        public ComponentSave GetComponent(string componentName)
        {
            GumProjectSave gps = GumProjectSave;

            if (gps != null)
            {
                foreach (ComponentSave componentSave in gps.Components)
                {
                    if (componentSave.Name == componentName)
                    {
                        return componentSave;
                    }
                }

            }

            return null;
        }

        public StandardElementSave GetStandardElement(string elementName)
        {
            GumProjectSave gps = GumProjectSave;

            if (gps != null)
            {
                foreach (StandardElementSave elementSave in gps.StandardElements)
                {
                    if (elementSave.Name == elementName)
                    {
                        return elementSave;
                    }
                }

            }

            return null;
        }








    }
}
