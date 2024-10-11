using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace FlatRedBall.Glue.Events
{
    /// <summary>
    /// Stores information about an Event type as defined in CSVs.
    /// </summary>
    /// <remarks>
    /// An EventSave is like a "type" for events.  It contains information
    /// such as the event name, the arguments, and so on.
    /// </remarks>
    public class EventSave
    {
        public string EventName;

        public string ConditionCode;

        public string Arguments;

        public string DelegateType;

        public bool CreatesEventMember;

        public string ExternalEvent;
    }
}
