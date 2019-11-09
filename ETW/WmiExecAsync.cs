using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using System;
using System.Collections.Generic;
using System.Text;

namespace WMIWatcher.ETW
{
     // <template tid = "task_012Args" >
     // < data name="GroupOperationId" inType="win:UInt32"/>
     // <data name = "Operation" inType="win:UnicodeString"/>
     // <data name = "HostId" inType="win:UInt32"/>
     // <data name = "ProviderName" inType="win:UnicodeString"/>
     // <data name = "ProviderGuid" inType="win:UnicodeString"/>
     // <data name = "Path" inType="win:UnicodeString"/>
     //</template>

    class WmiExecAsync : EventBase
    {
        public int GroupOperationId { get; }
        public WMIOperation Operation { get;  }

        public string WmiOperation { get; }
        public string ProviderName { get;  }

        public WmiExecAsync(TraceEvent @event) :base(@event)
        {
            Operation = (int)@event.ID switch
            {
                WMIProviderDefinitions.WMI_Activity_ExecAsync => WMIOperation.ExecAsync,
                _ => throw new InvalidOperationException($"Event with ID {WMIProviderDefinitions.WMI_Activity_ExecAsync} expected. But got: {@event.ID}")
            };
            GroupOperationId = (int) @event.PayloadByName("GroupOperationId");
            WmiOperation = (string)@event.PayloadByName("Operation");
            ProviderName = (string)@event.PayloadByName("ProviderName");
        }

        public override string ToString()
        {
            return $"Operation: {Operation}, WmiOperation: {WmiOperation}, GroupOperationId {GroupOperationId}";
        }
    }
}
