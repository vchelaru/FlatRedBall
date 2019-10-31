using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RenderingLibrary;

namespace TmxEditor
{
    public class ToolComponentManager
    {
        #region Fields

        static ToolComponentManager mSelf;

        List<ToolComponentBase> mComponents = new List<ToolComponentBase>();
        #endregion

        #region Properties

        public static ToolComponentManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new ToolComponentManager();
                }
                return mSelf;
            }
        }
        #endregion

        #region Register

        public void Register(ToolComponentBase component)
        {
            mComponents.Add(component);
        }

        #endregion

        public void ReactToLoadedFile(string fileName)
        {
            foreach (var component in mComponents.Where((component) => component.ReactToLoadedFile != null))
            {
                component.ReactToLoadedFile(fileName);
            }
        }

        public void ReactToXnaInitialize(SystemManagers managers)
        {
            foreach (var component in mComponents.Where((component) => component.ReactToXnaInitialize != null))
            {
                component.ReactToXnaInitialize(managers);
            }


        }

        public void ReactToWindowResize()
        {
            foreach (var component in mComponents.Where((component) => component.ReactToWindowResize != null))
            {
                component.ReactToWindowResize();
            }
        }

        public void ReactToLoadedAndMergedProperties(string fileName)
        {
            foreach (var component in mComponents.Where((component) => component.ReactToLoadedAndMergedProperties != null))
            {
                component.ReactToLoadedAndMergedProperties(fileName);
            }
        }
    }
}
