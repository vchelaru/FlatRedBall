using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Performance.Measurement;

namespace FlatRedBallProfiler.Sections
{
    public class SectionMerger
    {
        static SectionMerger mSelf;

        public static SectionMerger Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new SectionMerger();
                }
                return mSelf;
            }
        }

        public Section CreateMergedCopy(Section section)
        {
            Section toReturn = new Section();
            toReturn.Name = section.Name;
            toReturn.Time = section.Time;

            DoMerge(section, toReturn);

            toReturn.SetParentRelationships();
            return toReturn;
        }

        private void DoMerge(Section original, Section merged)
        {
            foreach (Section section in original.Children)
            {
                Section existingSection = GetSectionByName(section.Name, merged);

                if (existingSection == null)
                {
                    existingSection = new Section();
                    existingSection.Name = section.Name;
                    existingSection.Time = section.Time;
                    merged.Children.Add(existingSection);
                }
                else
                {
                    existingSection.Time += section.Time;
                }

                DoMerge(section, existingSection);

            }
        }


        Section GetSectionByName(string name, Section parent)
        {
            foreach (Section section in parent.Children)
            {
                if (section.Name == name)
                {
                    return section;
                }
            }
            return null;
        }
    }
}
