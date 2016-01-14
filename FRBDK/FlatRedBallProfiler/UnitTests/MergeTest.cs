using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FlatRedBall.Performance.Measurement;
using FlatRedBallProfiler.Sections;

namespace UnitTests
{
    [TestFixture]
    public class MergeTest
    {
        Section mSection;
        Section mMergedSection;

        [TestFixtureSetUp]
        public void Initialize()
        {
            mSection = new Section();

            mSection.Name = "A";
            mSection.Time = 100;

            for (int i = 0; i < 2; i++)
            {
                Section bSection = new Section();
                bSection.Name = "B";
                bSection.Time = 50;

                mSection.Children.Add(bSection);

                for (int j = 0; j < 2; j++)
                {
                    Section cSection = new Section();
                    cSection.Name = "C";
                    cSection.Time = 25;

                    bSection.Children.Add(cSection);

                }
            }

            mMergedSection = SectionMerger.Self.CreateMergedCopy(mSection);
        }



        [Test]
        public void Test()
        {
            if (mMergedSection == null)
            {
                throw new Exception("Merged section isn't properly being created by CreateMergedCopy");
            }
            if (mMergedSection.Children.Count != 1)
            {
                throw new Exception("Merged section doesn't properly have merged children");
            }

            if (mMergedSection.Children[0].Children.Count != 1)
            {
                throw new Exception("Merged section doesn't properly have merged grandchildren");
            }
            if (mMergedSection.Children[0].Parent != mMergedSection)
            {
                throw new Exception("Merged section doesn't properly set its parent");
            }

        }
    }
}
