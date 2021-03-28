using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Plugins;

namespace FlatRedBall.Glue.Reflection
{
    public class ExposableEvent : IComparable
    {
        public string Name;
        public string Variable;
        public BeforeOrAfter BeforeOrAfter;

        public ExposableEvent(string name)
        {
            Name = name;
        }

        public ExposableEvent(string name, string variable, BeforeOrAfter beforeOrAfter)
        {
            Name = name;
            Variable = variable;
            BeforeOrAfter = beforeOrAfter;
        }

        public override string ToString()
        {
            return Name;
        }

        public int CompareTo(object obj)
        {
            return this.Name.CompareTo(((ExposableEvent)obj).Name);
        }
    }

    public static class ExposedEventManager
    {

        public static List<ExposableEvent> GetExposableEventsFor(EntitySave entitySave, bool removeAlreadyExposed)
        {
            List<ExposableEvent> returnValues = new List<ExposableEvent>();

            // February 18, 2012
            // Not sure why this checks
            // both if the Entity is IClickable
            // and IWindow, but only IWindow has
            // these events.  Let's fix that:
            //if (entitySave.ImplementsIClickable || entitySave.ImplementsIWindow)
            if (entitySave.GetImplementsIWindowRecursively())
            {
                AddIWindowEvents(returnValues);

            }

            if (entitySave.GetImplementsIVisibleRecursively())
            {
                returnValues.Add(new ExposableEvent( "AfterVisibleSet", "Visible", BeforeOrAfter.After));
                returnValues.Add(new ExposableEvent("BeforeVisibleSet", "Visible", BeforeOrAfter.Before));
            }

            // todo:  Will want to implement after Enabled set events

            GetExposableEventsFor(entitySave, returnValues);


            returnValues.Sort(Sort);
            if (removeAlreadyExposed)
            {
                RemoveAlreadyExposed(entitySave, returnValues);
            }

            return returnValues;
        }

        private static void AddIWindowEvents(List<ExposableEvent> returnValues)
        {   
            returnValues.Add(new ExposableEvent("Click"));
            returnValues.Add(new ExposableEvent("ClickNoSlide"));
            returnValues.Add(new ExposableEvent("SlideOnClick"));
            returnValues.Add(new ExposableEvent("Push"));
            returnValues.Add(new ExposableEvent("DragOver"));
            returnValues.Add(new ExposableEvent("LosePush"));

            returnValues.Add(new ExposableEvent("RollOn"));
            returnValues.Add(new ExposableEvent("RollOff"));
        }

        static int Sort(ExposableEvent e1, ExposableEvent e2)
        {
            return e1.Name.CompareTo(e2.Name);
        }

        public static List<ExposableEvent> GetExposableEventsFor(ScreenSave screenSave, bool removeAlreadyExposed)
        {
            List<ExposableEvent> returnValues = new List<ExposableEvent>();

            GetExposableEventsFor(screenSave, returnValues);

            returnValues.Sort(Sort);

            if (removeAlreadyExposed)
            {
                RemoveAlreadyExposed(screenSave, returnValues);
            }

            return returnValues;
        }

        static void RemoveAlreadyExposed(IElement element, List<ExposableEvent> returnValues)
        {
            for (int i = returnValues.Count - 1; i > -1; i--)
            {
                string name = returnValues[i].Name;

                if (element.GetEvent(name) != null)
                {
                    returnValues.RemoveAt(i);
                }
            }

        }

        static void GetExposableEventsFor(IElement element, List<ExposableEvent> listToFill)
        {
            // We always want to have the BackPushed and StartPushed available:
            // Update June 4, 2013:
            // Not supplying StartPushed 
            // event anymore - too specific
            listToFill.Add(new ExposableEvent( "BackPushed"));
            //listToFill.Add(new ExposableEvent( "StartPushed"));
            listToFill.Add(new ExposableEvent("ResolutionOrOrientationChanged"));

            // Victor Chelaru May 12, 2013
            // I thought that the CSV could
            // drive events but it seems like
            // we have to manually create the events
            listToFill.Add(new ExposableEvent( "InitializeEvent"));

            // I think we need to manually create the event here?

            foreach (CustomVariable variable in element.CustomVariables)
            {
                if (variable.CreatesEvent)
                {
                    listToFill.Add(new ExposableEvent( "After" + variable.Name + "Set", variable.Name, BeforeOrAfter.After));
                    listToFill.Add(new ExposableEvent("Before" + variable.Name + "Set", variable.Name, BeforeOrAfter.Before));

                }
            }

            foreach (EventResponseSave ers in element.Events)
            {
                if (ers.GetIsTunneling() || 
                    ers.GetIsNewEvent()
                    )
                {
                    // this is something exposed, let's let the user expose this.
                    listToFill.Add(new ExposableEvent(ers.EventName));
                }
            }
        }


        public static List<ExposableEvent> GetExposableEventsFor(NamedObjectSave namedObjectSave, IElement parent)
        {
            List<ExposableEvent> returnValues = new List<ExposableEvent>();
            if (namedObjectSave != null)
            {
                switch (namedObjectSave.SourceType)
                {
                    case SourceType.Entity:
                        EntitySave entitySave = ObjectFinder.Self.GetEntitySave(namedObjectSave.SourceClassType);
                        if (entitySave == null)
                        {
                            // do nothing
                            //return returnValues;
                        }
                        else
                        {
                            returnValues = GetExposableEventsFor(entitySave, false);
                        }
                        break;
                    case SourceType.FlatRedBallType:
                        if (namedObjectSave.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.PositionedObjectList)
                        {
                            returnValues.Add(new ExposableEvent("CollectionChanged"));
                        }

                        break;
                    case SourceType.File:

                        var ati = namedObjectSave.GetAssetTypeInfo();

                        if (ati != null && ati.ImplementsIWindow)
                        {
                            AddIWindowEvents(returnValues);
                        }
                        break;
                }

                PluginManager.AddEventsForObject(namedObjectSave, returnValues);

            }
            if (parent != null)
            {
                for (int i = returnValues.Count - 1; i > -1; i--)
                {
                    bool found = false;
                    foreach (EventResponseSave ers in parent.Events)
                    {
                        if (ers.SourceObject == namedObjectSave.InstanceName &&
                            ers.SourceObjectEvent == returnValues[i].Name)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (found)
                    {
                        returnValues.RemoveAt(i);
                    }
                }
            }

            return returnValues;
        }
    }
}
