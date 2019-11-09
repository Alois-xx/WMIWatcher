using Microsoft.Diagnostics.Tracing.Etlx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WMIWatcher.ETW;

namespace WMIWatcher
{
    class WMIETlFileReader : IDisposable
    {
        TraceLog myLog;

        public WMIETlFileReader(string etlFile)
        {
            if (File.Exists(etlFile))
            {
                myLog = TraceLog.OpenOrConvert(etlFile);
            }
        }

        public List<WMIStart> GetPreviousStartEvents()
        {
            List<WMIStart> startEvents = new List<WMIStart>();

            if (myLog != null)
            {
                foreach (var ev in myLog.Events.Filter(x => (int)x.ID == WMIProviderDefinitions.WMI_Activity_Start))
                {
                    startEvents.Add(new WMIStart(ev, null, myLog));
                }
            }

            return startEvents;
        }

        public void Dispose()
        {
            myLog?.Dispose();
        }
    }
}
