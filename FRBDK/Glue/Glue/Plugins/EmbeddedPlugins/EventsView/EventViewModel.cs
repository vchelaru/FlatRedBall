using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace GlueControls.ViewModels.Event
{
    public class EventViewModel
    {
        IElement mElement;
        EventResponseSave mEventResponse;

        ObservableCollection<string> mAvailableSourceObjects = new ObservableCollection<string>();
        ObservableCollection<string> mAvailableEvents = new ObservableCollection<string>();

        public ObservableCollection<string> AvailableSourceObjects
        {
            get
            {
                return mAvailableSourceObjects;
            }
        }

        public ObservableCollection<string> AvailableEvents
        {
            get
            {
                return mAvailableEvents;
            }
        }

        public string SourceObject
        {
            get
            {
                if (mEventResponse != null)
                {
                    if (string.IsNullOrEmpty(mEventResponse.SourceObject))
                    {
                        return ThisName;
                    }
                    else
                    {
                        return mEventResponse.SourceObject;
                    }
                }
                else
                {
                    return null;
                }
            }
            set
            {
                // nothing yet
            }
        }

        public string SourceEventName
        {
            get
            {
                if (mEventResponse != null)
                {
                    return mEventResponse.EventName;
                }
                else
                {
                    return null;
                }
            }
            set
            {

            }
        }

        string ThisName
        {
            get
            {
                return "this (entire " + mElement.ClassName + ")";
            }
        }

        public void SetBackingObjects(GlueElement element, EventResponseSave eventResponse)
        {
            mElement = element;
            mEventResponse = eventResponse;

            RefreshLists();
        }

        private void RefreshLists()
        {
            RefreshAvailableSourceObjects();



            AvailableEvents.Clear();

            string sourceObject = SourceObject;

            NamedObjectSave nos = mElement.AllNamedObjects.FirstOrDefault(item => item.InstanceName == sourceObject);

            if (nos == null)
            {

                List<ExposableEvent> events = null;

                if (mElement is ScreenSave)
                {
                    events = ExposedEventManager.GetExposableEventsFor((ScreenSave)mElement, false);
                }
                else
                {
                    events = ExposedEventManager.GetExposableEventsFor((EntitySave) mElement, false);
                }

                foreach (var item in events)
                {
                    AvailableEvents.Add(item.Name);
                }
            }
            else
            {
                var events = ExposedEventManager.GetExposableEventsFor(nos, mElement);

                foreach (var item in events)
                {
                    AvailableEvents.Add(item.Name);
                }
            }

        }

        private void RefreshAvailableSourceObjects()
        {
            mAvailableSourceObjects.Clear();

            if (mElement != null)
            {
                mAvailableSourceObjects.Add(ThisName);

                foreach (var item in mElement.NamedObjects)
                {
                    mAvailableSourceObjects.Add(item.InstanceName);
                }
            }


        }



        public string SourceObjectEvent
        {
            get
            {
                return mEventResponse.EventName;
            }
            set
            {
                // nothing yet;
            }
        }
    }
}
