using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Topshelf;

namespace WMIWatcher
{
    public class Service : ServiceControl
    {
        WMIRealtimeReader myReader = new WMIRealtimeReader();
        object myLock = new object();
        
        /// <summary>
        /// Make it possible to cancel running serivce instance from Ctrl-C handler to 
        /// release running ETW Realtime session. If we do not do this you will leak
        /// Committed Memory which belongs to no one until you run out of memory and at some 
        /// point you cannot start ETW Sessions anymore. This happens sometimes when you break the application the hard way in the debugger 
        /// often enough to become a problem. Then you need to reboot your machine. 
        /// </summary>
        public static Service RunningService  = null;

        public bool Start(HostControl hostControl)
        {
            Task.Run(() => myReader.Process());
            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            Dispose();
            return true;
        }

        internal void Dispose()
        {
            // can be called from Ctrl-C handler or during service shutdown potentially from multiple threads
            // sync access
            lock (myLock)
            {
                if (myReader != null)
                {
                    myReader.Dispose();
                    myReader = null;
                }
            }
        }
    }
}
