using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using FlatRedBall.IO;

namespace EditorObjects.Data
{
    public class RuntimeOptions
    {
        #region Properties

        public int WindowWidth
        {
            get;
            set;
        }

        public int WindowHeight
        {
            get;
            set;
        }

        public bool FullScreen
        {
            get;
            set;
        }

        #endregion

        #region Methods

#if FRB_MDX

        public void Apply()
        {
            EditorWindow.LastInstance.Width = WindowWidth;
            EditorWindow.LastInstance.Height = WindowHeight;
        }
#endif
        public static RuntimeOptions FromFile(string fileName)
        {
            return FileManager.XmlDeserialize<RuntimeOptions>(fileName);
        }

        public void Save(string fileName)
        {
            FileManager.XmlSerialize(this, fileName);
        }

        #endregion
    }
}
