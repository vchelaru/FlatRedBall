using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.IO;
using FlatRedBall.Performance.Measurement;
using FlatRedBallProfiler.Managers;
using FlatRedBallProfiler.Sections;

namespace FlatRedBallProfiler
{
    public class ProjectManager
    {
        #region Fields

        Section mSection;

        Section mMergedSection;

        static ProjectManager mSelf;

        #endregion

        #region Properties

        public Section Section
        {
            get { return mSection; }
            set
            {
                mSection = value;

                mMergedSection = SectionMerger.Self.CreateMergedCopy(mSection);
                mSection.SetParentRelationships();
                TreeViewManager.Self.RefreshUI();
            }
        }

        public Section MergedSection
        {
            get { return mMergedSection; }
        }

        public static ProjectManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new ProjectManager();
                }
                return mSelf;
            }
        }

        public string LastFile
        {
            get;
            private set;
        }

        #endregion


        public void Load(string file)
        {

            Section = Section.FromBase64GzipFile(file);
            
            LastFile = file;
        }

        public void LoadFromString(string contents)
        {
            Section = FileManager.XmlDeserializeFromString<Section>(contents);


            LastFile = "From clipboard";
        }
        
    }
}
