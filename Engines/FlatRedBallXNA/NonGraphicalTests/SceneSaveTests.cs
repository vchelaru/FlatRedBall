using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FlatRedBall.Content;

namespace NonGraphicalTests
{
    [TestFixture]
    public class SceneSaveTests
    {
        [Test]
        public void TestLoading()
        {
            SpriteEditorScene.ManualDeserialization = true;
            SpriteEditorScene.FromFile("Content/BorwnClayout.scnx");


        }
    }
}
