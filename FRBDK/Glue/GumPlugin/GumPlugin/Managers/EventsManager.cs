using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using GumPlugin.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GumPlugin.Managers
{
    public class EventsManager : Singleton<EventsManager>
    {
        List<FlatRedBall.Glue.Events.EventSave> mEventsAdded = new List<FlatRedBall.Glue.Events.EventSave>();

        internal void HandleAddEventsForObject(NamedObjectSave namedObject, List<ExposableEvent> listToFill)
        {
            bool shouldAddEventsForThis, shouldAddEventsForChildren;
            GetIfShouldAddEvents(namedObject, out shouldAddEventsForThis, out shouldAddEventsForChildren);

            if (shouldAddEventsForThis)
            {
                listToFill.Add(new ExposableEvent("Click"));
                listToFill.Add(new ExposableEvent("RollOver"));
                listToFill.Add(new ExposableEvent("RollOn"));
                listToFill.Add(new ExposableEvent("RollOff"));
            }
            if (shouldAddEventsForChildren)
            {
                string strippedName = FileManager.RemoveExtension(FileManager.RemovePath(namedObject.SourceFile));

                var element = 
                    namedObject.GetAssetTypeInfo()?.Tag as ElementSave ??
                    AppState.Self.AllLoadedElements.FirstOrDefault(item =>
                        item.Name.ToLowerInvariant() == strippedName.ToLowerInvariant());

                if(element != null)
                {
                    string instanceName = namedObject.SourceNameWithoutParenthesis;

                    if (!string.IsNullOrEmpty(instanceName) && instanceName != "this")
                    {
                        var instance = element.Instances.FirstOrDefault(item => item.Name == instanceName);
                        if(instance != null)
                        {
                            element = Gum.Managers.ObjectFinder.Self.GetElementSave(instance);
                        }
                    }

                    if(element != null)
                    {
                        var events = EventCodeGenerator.Self.GetExposedChildrenEvents(element);
                        foreach(var childEvent in events)
                        {
                            var exposableEvent =
                                new ExposableEvent(childEvent.ExposedAsName);
                            
                            listToFill.Add(exposableEvent);
                        }
                    }
                }
            }

        }

        private static void GetIfShouldAddEvents(NamedObjectSave namedObject, out bool shouldAddEventsForThis, out bool shouldAddEventsForChildren)
        {
            shouldAddEventsForThis = false;
            shouldAddEventsForChildren = false;
            if (namedObject.SourceType == SourceType.File)
            {
                string extension = FileManager.GetExtension(namedObject.SourceFile);

                if (extension == GumProjectSave.ComponentExtension ||
                    extension == GumProjectSave.ProjectExtension ||
                    extension == GumProjectSave.ScreenExtension ||
                    extension == GumProjectSave.StandardExtension)
                {
                    string strippedName = FileManager.RemoveExtension(FileManager.RemovePath(namedObject.SourceFile));

                    var element = AppState.Self.AllLoadedElements.FirstOrDefault(item =>
                        item.Name.ToLowerInvariant() == strippedName.ToLowerInvariant());

                    AssignShouldBoolsForAti(namedObject, ref shouldAddEventsForThis, ref shouldAddEventsForChildren, element);
                }
            }
            else if(namedObject.SourceType == SourceType.FlatRedBallType || namedObject.SourceType == SourceType.Gum)
            {
                var ati = namedObject.GetAssetTypeInfo();

                var isGumAti = AssetTypeInfoManager.Self.IsAssetTypeInfoGum(ati);

                if(isGumAti)
                {
                    var element = ati.Tag as ElementSave;

                    AssignShouldBoolsForAti(namedObject, ref shouldAddEventsForThis, ref shouldAddEventsForChildren, element);
                }
            }

            static void AssignShouldBoolsForAti(NamedObjectSave namedObject, ref bool shouldAddEventsForThis, ref bool shouldAddEventsForChildren, ElementSave element)
            {
                if (element != null)
                {
                    string instanceName = namedObject.SourceNameWithoutParenthesis;

                    object hasEventsAsObject;
                    object exposeChildrenEventsAsObject;

                    // Instance can be null if we are dealing with a Glue NamedObjectSave with a Gum (or FlatRedBall) source type rather than File.
                    // In this case it's an instance created wholely in Glue rather than an object gabbed out of Gum.
                    if (string.IsNullOrEmpty(instanceName) || instanceName == "this")
                    {
                        hasEventsAsObject = element.GetValueFromThisOrBase("HasEvents");
                        exposeChildrenEventsAsObject = element.GetValueFromThisOrBase("ExposeChildrenEvents");

                    }
                    else
                    {
                        var instance = element.Instances.FirstOrDefault(item => item.Name == instanceName);

                        hasEventsAsObject = instance.GetValueFromThisOrBase(element, "HasEvents");
                        exposeChildrenEventsAsObject = instance.GetValueFromThisOrBase(element, "ExposeChildrenEvents");
                    }

                    if (hasEventsAsObject != null && hasEventsAsObject is bool)
                    {
                        shouldAddEventsForThis = (bool)hasEventsAsObject;
                    }
                    if (exposeChildrenEventsAsObject != null && exposeChildrenEventsAsObject is bool)
                    {
                        shouldAddEventsForChildren = (bool)exposeChildrenEventsAsObject;
                    }
                }
            }
        }

        internal void RefreshEvents()
        {
            var allGlueEvents = FlatRedBall.Glue.Events.EventManager.AllEvents;
            var kvpList = allGlueEvents.Where(item => mEventsAdded.Contains(item.Value)).ToList();

            foreach (var key in kvpList.Select(item => item.Key))
            {
                allGlueEvents.Remove(key);
            }

            // Now let's re-add
            foreach (var element in AppState.Self.AllLoadedElements)
            {
                foreach (var gumEvent in element.Events.Where(item => item.Enabled))
                {
                    if (!allGlueEvents.ContainsKey(gumEvent.GetExposedOrRootName()))
                    {
                        var glueEventSave = new FlatRedBall.Glue.Events.EventSave();

                        glueEventSave.Arguments = "FlatRedBall.Gui.IWindow callingWindow";
                        glueEventSave.DelegateType = "FlatRedBall.Gui.WindowEvent";


                        allGlueEvents.Add(gumEvent.GetExposedOrRootName(), glueEventSave);
                    }

                }
            }

        }

    }

    static class ElementSaveExtensionMethods
    {
        public static object GetValueFromThisOrBase(this ElementSave element, string variable, bool forceDefault = false)
        {
            var stateToPullFrom = element.DefaultState;

            var variableSave = stateToPullFrom.GetVariableRecursive(variable);
            if (variableSave != null)
            {
                return variableSave.Value;
            }
            else
            {
                return null;
            }
        }
    }
}
