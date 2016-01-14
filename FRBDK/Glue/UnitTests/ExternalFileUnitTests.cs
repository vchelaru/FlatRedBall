using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FlatRedBall.Glue;
using EditorObjects.SaveClasses;
using FlatRedBall.Glue.Managers;

namespace UnitTests
{
    [TestFixture]
    public class ExternalFileUnitTests
    {
        [TestFixtureSetUp]
        public void Initialize()
        {
            BuildToolAssociation bta = new BuildToolAssociation(){
                SourceFileType = "src",
                DestinationFileType = "dst",
                BuildTool = "c:\\WHATEVER.exe"};

            BuildToolAssociationManager.Self.ProjectSpecificBuildTools = new BuildToolAssociationList();

            BuildToolAssociationManager.Self.ProjectSpecificBuildTools.BuildToolList.Add(bta);
        }


        [Test]
        public void Test()
        {

        }
    }
}
