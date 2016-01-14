using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.Settings
{
    [XmlRoot("PreferencesSave")]
    public class GlueLayoutSettings
    {
        #region Fields
        private int mLeftPanelSplitterPosition = 200;
        private int mRightPanelSplitterPosition = 1000;
        private int mTopPanelSplitterPosition = 200;
        private int mBottomPanelSplitterPosition = 1000;
        private bool mMaximized = false;

        const string filename = "GlueLayoutSettings.xml";

        #endregion

        public int LeftPanelSplitterPosition
        {
            get { return mLeftPanelSplitterPosition; }
            set { mLeftPanelSplitterPosition = value; }
        }

        public int RightPanelSplitterPosition
        {
            get { return mRightPanelSplitterPosition; }
            set { mRightPanelSplitterPosition = value; }
        }

        public int TopPanelSplitterPosition
        {
            get { return mTopPanelSplitterPosition; }
            set { mTopPanelSplitterPosition = value; }
        }

        public int BottomPanelSplitterPosition
        {
            get { return mBottomPanelSplitterPosition; }
            set { mBottomPanelSplitterPosition = value; }
        }

        public bool Maximized
        {
            get { return mMaximized; }
            set { mMaximized = value; }
        }

        [XmlIgnore]
        public static bool StopSavesAndLoads { get; set; }

        public static GlueLayoutSettings LoadSettings()
        {
            if(StopSavesAndLoads)
                return new GlueLayoutSettings();

            string fileName = FileManager.UserApplicationDataForThisApplication + filename;

            if (FileManager.FileExists(fileName))
            {
                GlueLayoutSettings pS = FileManager.XmlDeserialize<GlueLayoutSettings>(fileName);
                return pS;
            }
            
            return new GlueLayoutSettings();
        }

        public void SaveSettings()
        {
            if(!StopSavesAndLoads)
                FileManager.XmlSerialize(this, FileManager.UserApplicationDataForThisApplication + filename);
        }
    }
}
