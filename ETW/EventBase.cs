using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Text;

namespace WMIWatcher.ETW
{
    public class EventBase
    {
        public DateTime TimeStamp { get; }

        public EventBase(TraceEvent @event)
        {
            TimeStamp = @event.TimeStamp;
        }
    }
}
