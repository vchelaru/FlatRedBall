using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Utilities;

namespace FlatRedBall.Glue.Events
{
    public interface IEventContainer : INameable
    {

        List<EventResponseSave> Events
        {
            get;
            set;
        }

        EventResponseSave GetEvent(string eventName);
    }
}
