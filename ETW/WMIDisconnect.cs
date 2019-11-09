using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Text;

namespace WMIWatcher.ETW
{
    public class WmiDisconnect : EventBase
    {
        public int OperationId { get; }
        public WMIOperation Operation { get; }

        public WmiDisconnect(TraceEvent @event) : base(@event)
        {
            OperationId = (int)@event.ID switch
            {
                WMIProviderDefinitions.WMI_Activity_Disconnect => (int)@event.PayloadByName("OperationId"),
                _ => throw new InvalidOperationException($"Event with ID {WMIProviderDefinitions.WMI_Activity_Disconnect} expected. But got: {@event.ID}")
            };

            Operation = WMIOperation.Disconnect;
        }
    }

}
