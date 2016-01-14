using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using NUnit.Framework;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.IO;
using FlatRedBall.Glue.CodeGeneration;
using System.IO;
using FlatRedBall.Glue.SaveClasses.Helpers;

namespace UnitTests
{
    [TestFixture]
    public class EventTests
    {
        #region Fields

        EntitySave mEntitySave;
        EntitySave mDerivedEntitySave;
        ScreenSave mScreenSave;
        NamedObjectSave mListNos;

        #endregion

        [TestFixtureSetUp]
        public void Initialize()
        {
            OverallInitializer.Initialize();

            mEntitySave = new EntitySave();
            mEntitySave.ImplementsIWindow = true;
            mEntitySave.Name = "EventTestEntity";
            mEntitySave.ImplementsIWindow = true;
            ObjectFinder.Self.GlueProject.Entities.Add(mEntitySave);

            mScreenSave = new ScreenSave();
            mScreenSave.Name = "EventTestScreen";
            ObjectFinder.Self.GlueProject.Screens.Add(mScreenSave);

            NamedObjectSave nos = new NamedObjectSave();
            nos.SourceType = SourceType.Entity;
            nos.SourceClassType = "EventTestEntity";
            mScreenSave.NamedObjects.Add(nos);


            EventResponseSave ers = new EventResponseSave();
            ers.SourceObject = "EventTestEntity";
            ers.SourceObjectEvent = "Click";
            ers.EventName = "EventTestEntityClick";
            mScreenSave.Events.Add(ers);

            EventResponseSave pushErs = new EventResponseSave();
            pushErs.SourceObject = "EventTestEntity";
            pushErs.SourceObjectEvent = "Push";
            pushErs.EventName = "EventTestEntityPush";
            mScreenSave.Events.Add(pushErs);

            // Create a POList so we can expose its event(s)
            mListNos = new NamedObjectSave();
            mListNos.SourceType = SourceType.FlatRedBallType;
            mListNos.SourceClassType = "PositionedObjectList<T>";
            mListNos.SourceClassGenericType = "Sprite";
            mScreenSave.NamedObjects.Add(mListNos);

            mDerivedEntitySave = new EntitySave();
            mDerivedEntitySave.Name = "EventTestsDerivedEntity";
            mDerivedEntitySave.BaseEntity = mEntitySave.Name;
            ObjectFinder.Self.GlueProject.Entities.Add(mDerivedEntitySave);
        }

        [Test]
        public void TestExposedEvents()
        {
            List<ExposableEvent> exposableEvents = ExposedEventManager.GetExposableEventsFor(mScreenSave, false);

            if(exposableEvents.Count < 4)
            {
                throw new Exception("Exposable events for Screens are not working properly");
            }

            exposableEvents = ExposedEventManager.GetExposableEventsFor(mListNos, mScreenSave);
            if (exposableEvents.ContainsName("CollectionChanged") == false)
            {
                throw new Exception("Exposable events aren't exposing CollectionChanged on PositionedObjectLists");
            }

            exposableEvents = ExposedEventManager.GetExposableEventsFor(mDerivedEntitySave, false);
            if (!exposableEvents.ContainsName("Click"))
            {
                throw new Exception("Exposed events are not considering inheritance and they should!");
            }
        }

        [Test]
        public void TestRenamingEvents()
        {
            EventResponseSave ers = new EventResponseSave();
            ers.EventName = "Whatever";
            ers.DelegateType = "System.EventHandler";
            mEntitySave.Events.Add(ers);
            string fileName = ers.GetSharedCodeFullFileName();

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            string directory = FileManager.CurrentDirectory + fileName;

            string contents = "int m = 33;";


            string contentsFileName = EventCodeGenerator.InjectTextForEventAndSaveCustomFile(
                mEntitySave, ers, contents);

            string entireFileContents = FileManager.FromFileText(contentsFileName);

            // Make sure that this file contains the contents
            if (!entireFileContents.Contains(contents))
            {
                throw new Exception("The entire files aren't being added to the event");
            }

            // Test renaming now
            string oldName = ers.EventName;

            string newName = "AfterRename";
            ers.EventName = newName;

            // I can't write unit tests for this yet because it requires that
            // all generate code be moved out of BaseElementTreeNode into CodeWriter:
            //EventResponseSavePropertyChangeHandler.Self.ReactToChange("EventName", oldName, ers, mEntitySave);

            //entireFileContents = FileManager.FromFileText(contentsFileName);

        }

    }

    public static class ExposableEventExtensionMethods
    {
        public static bool ContainsName(this List<ExposableEvent> list, string name)
        {
            foreach (ExposableEvent item in list)
            {
                if (item.Name == name)
                {
                    return true;
                }
            }

            return false;

        }


    }


}
