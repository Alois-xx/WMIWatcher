using System;
using System.Collections.Generic;
using System.Text;

namespace WMIWatcher
{
    class Row
    {
        public static string Print(params string[] strings)
        {
            StringBuilder sb = new StringBuilder();
            for(int i=0;i<strings.Length;i++)
            {
                sb.Append(strings[i]);
                if( i != strings.Length-1)
                {
                    sb.Append("|");
                }
            }

            string msg = sb.ToString();
            Console.WriteLine(msg);
            return msg;
        }
    }
}
