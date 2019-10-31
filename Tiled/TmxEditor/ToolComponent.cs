using System;
using RenderingLibrary;

namespace TmxEditor
{

    public abstract class ToolComponentBase
    {
        public Action<string> ReactToLoadedFile;
        public Action<string> ReactToLoadedAndMergedProperties;
        public Action<SystemManagers> ReactToXnaInitialize;
        public Action ReactToWindowResize;
    }

    public abstract class ToolComponent<T> : ToolComponentBase where T : class, new()
    {
        static T mSelf;

        public static T Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new T();
                }
                return mSelf;
            }
        }
    }
}
