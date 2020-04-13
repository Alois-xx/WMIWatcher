using System;
using System.Collections.Generic;
using System.Management;
using System.Text;

namespace WMIWatcher.WMI
{
    class WmiQuery
    {
        string myQuery;
        string myWmiNamespace;

        public WmiQuery(string query, string wmiNameSpace)
        {
            query = query.Trim();
            if (query.StartsWith("select", StringComparison.OrdinalIgnoreCase))
            {
                myQuery = query;
                myWmiNamespace = wmiNameSpace;
            }
        }

        public void Execute()
        { 
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(myWmiNamespace, myQuery);

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    foreach(var prop in queryObj.Properties)
                    {
                        Console.WriteLine($"[{prop.Name}]: {prop.Value}");
                    }
                }
            }
            catch (ManagementException e)
            {

            }
        }
    }
}
