using System;
using System.Collections.Generic;
using System.Text;

namespace WMIWatcher.ETW
{
    public enum WMIOperation
    {
        Other,
        Connect,
        ExecQuery,
        ExecMethod,
        ExecAsync,
        CreateEnumerator,
        Disconnect,
        NotificationQuery,
        ProcessEnd,
    }
}
