using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;

namespace WMIWatcher.WMI
{
    class WmiQuery
    {
        readonly string myQuery;
        readonly string myWmiNamespace;

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
                ManagementObjectSearcher searcher = new(myWmiNamespace, myQuery);

                foreach (ManagementObject queryObj in searcher.Get().Cast<ManagementObject>())
                {
                    foreach(var prop in queryObj.Properties)
                    {
                        Console.WriteLine($"[{prop.Name}]: {prop.Value}");
                    }
                }
            }
            catch (ManagementException)
            {

            }
        }
    }
}
