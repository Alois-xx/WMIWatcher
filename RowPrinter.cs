using System;
using System.Text;

namespace WMIWatcher
{
    class Row
    {
        static char[] myTrimChars = new char[] { '\r', '\n' };

        public static string Print(params string[] strings)
        {
            StringBuilder sb = new StringBuilder();
            for(int i=0;i<strings.Length;i++)
            {
                // prevent multiline rows and replace any column separators with #
                sb.Append(strings[i]?.Trim(myTrimChars).Replace('|','#'));
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
