using Microsoft.Diagnostics.Tracing;
using System;

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
