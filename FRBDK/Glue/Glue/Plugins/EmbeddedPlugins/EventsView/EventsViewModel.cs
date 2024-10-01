using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace GlueControls.ViewModels.Event
{
    public class EventsViewModel
    {
        ObservableCollection<EventViewModel> mColumns = new ObservableCollection<EventViewModel>();

        GlueElement mBackingElement;

        //IEnumerable<EventResponseSave> mBackingEvents;

        //public IEnumerable<EventResponseSave> BackingEvents
        //{
        //    get { return mBackingEvents; }
        //    set 
        //    { 
        //        mBackingEvents = value;

        //        RefreshColumns();
        //    }
        //}

        public GlueElement BackingElement
        {
            get { return mBackingElement; }
            set
            {
                mBackingElement = value;

                RefreshColumns();
            }
        }

        public IEnumerable<EventViewModel> Columns
        {
            get
            {
                return mColumns;

            }
        }

        public EventsViewModel()
        {

        }


        private void RefreshColumns()
        {
            mColumns.Clear();
            foreach (var ers in mBackingElement.Events)
            {
                EventViewModel evm = new EventViewModel();
                evm.SetBackingObjects(mBackingElement, ers);

                mColumns.Add(evm);
            }
        }


    }
}
