using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using WMIWatcher.ETW;

namespace WMIWatcher
{
    public class WMIRealtimeReader : IDisposable
    {
        /// <summary>
        /// Realtime session
        /// </summary>
        TraceEventSession myRealtimeSession;

        /// <summary>
        /// Realtime ETW event stream processor
        /// </summary>
        TraceLogEventSource myTraceLogSource;


        readonly int myPid = System.Diagnostics.Process.GetCurrentProcess().Id;

        /// <summary>
        /// At boot time our service does not yet run. To get all events we start a WMI Autologger user mode session which no kernel (process start) events
        /// When our service starts we parse these events and put them into the myEvents collection which is processed when our Realtime ETW session with full
        /// process names arrives. That way we can later for most processes assign full process names and not only pids from the early AutoLogger session
        /// </summary>
        const string AutoLoggerFileName = "%SystemRoot%\\System32\\LogFiles\\WMI\\WmiWatcher.etl";

        /// <summary>
        /// Restart ETW Realtime watcher to prevent consuming too much memory over time by previously started but now stopped processes.
        /// </summary>
        readonly TimeSpan RestartTime = TimeSpan.FromHours(20);

        /// <summary>
        /// Parsed events from Autologger session which starts at boot until our service starts which stops the session and starts a new Realtime session
        /// </summary>
        readonly List<WMIStart> myEvents = new List<WMIStart>();
        

        /// <summary>
        /// Keep all processes which did start a WMI query in a hashset. When a process which did issue a WMI query does
        /// end we can log the process runtime duration which is sometimes useful for processes which do run one or more WMI queries and then exit
        /// </summary>
        HashSet<KeyValuePair<string, int>> myProcessCmdLineWithPids = new HashSet<KeyValuePair<string, int>>();

        public WMIRealtimeReader()
        {
        }

        public void Process()
        {
            while (true)
            {
                var processing = Task.Run(ProcessData);
                if (processing.Wait(RestartTime) == false)
                {
                    FileLogger.Logger.Log("Restarting ETW Monitoring to prevent memory leaks");
                    myRealtimeSession?.Stop();
                    myTraceLogSource.Dispose();
                    processing.Wait();
                    FileLogger.Logger.Log("Current ETW Monitoring was successfully stopped.");
                }
            }
        }

        void ProcessData()
        {
            try
            {
                using (myRealtimeSession = new TraceEventSession("WMIWatcher_Realtime"))
                {
                    myRealtimeSession.EnableKernelProvider(KernelTraceEventParser.Keywords.ImageLoad | KernelTraceEventParser.Keywords.Process, KernelTraceEventParser.Keywords.None);
                    myRealtimeSession.EnableProvider(WMIProviderDefinitions.WMI_Activity_Provider_Name, TraceEventLevel.Verbose, WMIProviderDefinitions.Keyword_WMI_Activity_Trace);

                    using (myTraceLogSource = TraceLog.CreateFromTraceEventSession(myRealtimeSession))
                    {
                        WMIEventParser parser = new WMIEventParser(myTraceLogSource, "WmiWatcher", AutoLoggerFileName);
                        myTraceLogSource.Dynamic.All += parser.Parse;
                        parser.OnWMIOperationStart += Parser_OnWMIOperationStart;
                        //parser.OnWMIOperationStop += Parser_OnWMIOperationStop;
                        parser.OnWMIExecAsync += Parser_OnWMIExecAsync;
                        parser.OnProcessEndedWithDuration += Parser_OnProcessEndedWithDuration;

                        myTraceLogSource.Process();
                    }
                }
            }
            catch(Exception ex)
            {
                FileLogger.Logger.Log($"Error: Got Exception while leaving ProcessData: {ex}");
                throw;
            }
        }

        public void Dispose()
        {
            myTraceLogSource.Dispose();
        }

        private void Parser_OnProcessEndedWithDuration(Microsoft.Diagnostics.Tracing.Parsers.Kernel.ProcessTraceData endEvent, TimeSpan processDuration)
        {
            var kvp = new KeyValuePair<string, int>(endEvent.CommandLine, endEvent.ProcessID);
            if( myProcessCmdLineWithPids.Contains(kvp))
            {
                myProcessCmdLineWithPids.Remove(kvp);
                string msg = Row.Print(
                 DateString(endEvent.TimeStamp),
                 TimeString(endEvent.TimeStamp),
                 WMIOperation.ProcessEnd.ToString(),
                 endEvent.CommandLine,
                 endEvent.ProcessID.ToString(),
                 null,
                 null,
                 null,
                 null,
                 null,
                 processDuration.TotalSeconds.ToString("F1"));
                FileLogger.Logger.Log(msg);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        private void Parser_OnWMIOperationStart(WMIStart obj)
        {
            myEvents.Add(obj);
            myProcessCmdLineWithPids.Add(new KeyValuePair<string, int>(obj.ClientProcess, obj.ClientProcessId));
            if (obj.ClientProcessId != myPid && obj.Operation != WMIOperation.Connect)
            {
                string msg = Row.Print(
                    DateString(obj.TimeStamp),
                    TimeString(obj.TimeStamp),
                    obj.Operation.ToString(),
                    obj.ClientProcess,
                    obj.ClientProcessId.ToString(),
                    obj.IsRemoteQuery.ToString(),
                    obj.Query,
                    obj.NameSpace,
                    obj.OperationId.ToString(),
                    obj.GroupOperationid.ToString(),
                    null);

                FileLogger.Logger.Log(msg);
            }
        }

        /// <summary>
        /// Every sync execution is followed by and async execution of the query.
        /// The only exception are polling WMI queries (WITHIN xxx) which are executed always as async events
        /// you can recognize therefore WMI polling queries as async events which no preceding sync execution
        /// </summary>
        /// <param name="obj"></param>
        private void Parser_OnWMIExecAsync(WmiExecAsync obj)
        {
            string msg = Row.Print(
              DateString(obj.TimeStamp),
              TimeString(obj.TimeStamp),
              obj.Operation.ToString(),
              null,
              null,
              null,
              obj.WmiOperation,
              null,
              0.ToString(),
              obj.GroupOperationId.ToString(),
              null);
            FileLogger.Logger.Log(msg);
        }

        private void Parser_OnWMIOperationStop(WmiDisconnect obj)
        {
            if( myEvents.Count == 10 * 1000)
            {
                myEvents.Clear();
            }

            // find query with same OperationId for the Disconnect event
            // this may have something to do with the query duration but not necessarily because
            // the connection can be pooled such as in e.g. Powershell where a disconnect does not happen or much later.
            string query = "N.a.";
            for(int i=myEvents.Count-1;i>=0;i--)
            {
                var start = myEvents[i];
                if( obj.OperationId == start.OperationId )
                {
                    query = start.Query;
                    break;
                }
            }

            string msg = Row.Print(
                DateString(obj.TimeStamp),
                TimeString(obj.TimeStamp), 
                obj.Operation.ToString(), 
                null, 
                null, 
                null, 
                query, 
                null, 
                obj.OperationId.ToString(),
                null,
                null);
            FileLogger.Logger.Log(msg);
        }


        string TimeString(DateTime time)
        {
            return time.ToString("HH:mm:ss.fff");
        }

        string DateString(DateTime time)
        {
            return time.ToString("yyyy.MM.dd");
        }
    }
}
