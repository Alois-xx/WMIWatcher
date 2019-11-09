using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WMIWatcher.ETW;

namespace WMIWatcher
{
    class WMIEventParser
    {
        public event Action<WMIStart> OnWMIOperationStart;
        public event Action<WmiDisconnect> OnWMIOperationStop;
        public event Action<WmiExecAsync> OnWMIExecAsync;
        public event Action<ProcessTraceData, TimeSpan> OnProcessEndedWithDuration;

        readonly TraceLogEventSource mySource;
        List<WMIStart> myUnprocessedEvents;

        List<ProcessTraceData> myProcessStartEvents = new List<ProcessTraceData>();

        public WMIEventParser(TraceLogEventSource source, string autoLoggerSessionName, string autoLoggerFileName)
        {
            mySource = source;
            source.Kernel.ProcessStart += Kernel_ProcessStart;
            source.Kernel.ProcessStop += Kernel_ProcessStop;

            // stop already started autologger sessoin which runs since boot
            // this way we capture all WMI events even the ones which did happen while our service was already starting!
            // It can happen that we duplicate some events but that is still better than to loose important polling events
            // which can happen in services which run early in the boot phase.
            var fileSession = TraceEventSession.GetActiveSession(autoLoggerSessionName);
            fileSession?.Stop();
            // Only parse when session was still running otherwise we would inject old events upon session restart again
            if (fileSession != null)
            {
                using WMIETlFileReader reader = new WMIETlFileReader(Environment.ExpandEnvironmentVariables(autoLoggerFileName));
                myUnprocessedEvents = reader.GetPreviousStartEvents();
            }
        }

        private void Kernel_ProcessStart(ProcessTraceData obj)
        {
            myProcessStartEvents.Add((ProcessTraceData)obj.Clone());
        }

        /// <summary>
        /// Calculate process duration and fire an event when a process did terminate
        /// </summary>
        /// <param name="obj"></param>
        private void Kernel_ProcessStop(ProcessTraceData obj)
        {
            ProcessTraceData start;
            for (int i= myProcessStartEvents.Count-1; i>=0;i--)
            {
                start = myProcessStartEvents[i];
                if( obj.ProcessID == start.ProcessID && obj.CommandLine == start.CommandLine)
                {
                    OnProcessEndedWithDuration?.Invoke(obj, obj.TimeStamp - start.TimeStamp);
                    break;
                }
            }
        }

        public void Parse(TraceEvent data)
        {
            ProcessEventsFromAutoLoggerFirst();

            if (data.ProviderName == WMIProviderDefinitions.WMI_Activity_Provider_Name)
            {
                switch ((int)data.ID)
                {
                    case WMIProviderDefinitions.WMI_Activity_Start:
                        OnWMIOperationStart?.Invoke(new WMIStart(data, mySource, null));
                        break;
                    case WMIProviderDefinitions.WMI_Activity_Disconnect:
                        OnWMIOperationStop?.Invoke(new WmiDisconnect(data));
                        break;
                    case WMIProviderDefinitions.WMI_Activity_ExecAsync:
                        OnWMIExecAsync?.Invoke(new WmiExecAsync(data));
                        break;
                    case WMIProviderDefinitions.WMI_Activity_Transfer:
                        break;
                    default:
                        break;
                };
            }
        }

        private void ProcessEventsFromAutoLoggerFirst()
        {
            if (myUnprocessedEvents != null && myUnprocessedEvents.Count > 0)
            {
                // since the AutogLogger session has no kernel session attached we only get raw process ids
                // To work around that we fill in the process names from still running processes from the realtime session
                foreach (WMIStart wmiStartEvent in myUnprocessedEvents)
                {
                    TraceProcess process = mySource?.TraceLog.Processes.Where(p => p.ProcessID == wmiStartEvent.ClientProcessId).FirstOrDefault();
                    if (process != null)
                    {
                        wmiStartEvent.ClientProcess = process.CommandLine;
                    }
                    OnWMIOperationStart?.Invoke(wmiStartEvent);
                }
                myUnprocessedEvents.Clear();
            }
        }
    }
}
