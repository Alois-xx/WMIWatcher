using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;



namespace WMIWatcher
{
    class FileLogger : IDisposable
    {
        object myLock = new object();
        StreamWriter myLog;
        FileStream myFile;
        internal const string myFileBaseName = "WMIWatcher.csv";
        internal string myLoggingDirectory;
        long myMaxFileSizeInBytes;
        int myMaxGenerationsToKeep;

        public FileLogger(string loggingDirectory, int maxFileSizeInMB, int maxGenerationsToKeep)
        {
            myMaxFileSizeInBytes = maxFileSizeInMB * 1024 * 1024L;
            myMaxGenerationsToKeep = maxGenerationsToKeep;
            myLoggingDirectory = loggingDirectory ?? Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

            ReopenFileAndWriteHeader();
        }

        private void ReopenFileAndWriteHeader()
        {
            string logFile = Path.Combine(myLoggingDirectory, myFileBaseName);
            myFile = null;
            for (int i = 0; i < 2; i++)
            {
                try
                {
                    myFile = new FileStream(logFile, FileMode.Append, FileAccess.Write, FileShare.Read);
                    break;
                }
                catch (Exception)
                {
                    logFile = Path.Combine(Path.GetDirectoryName(logFile), $"{myFileBaseName}_{DateTime.Now.ToString("hh_MM_ss.fff").ToString()}.csv");
                }
            }

            myLog = new StreamWriter(myFile);

            // only write header when new file was created
            if (myFile.Position == 0)
            {
                WriteHeader();
            }
        }

        void WriteHeader()
        {
            myLog.WriteLine("sep=|"); // Excel recognizes this as default separator
            string columnDescription = Row.Print("Date", "Time", "Operation", "ClientProcess", "ClientProcessId", "IsRemote", "Query", "NameSpace", "OperationId", "GroupOperationId", "Duration s");
            myLog.WriteLine(columnDescription);
        }

        internal void RollOver()
        {
            string fileName = myFile.Name;
            myLog.Close();
            string dir = Path.GetDirectoryName(fileName);
            string ext = Path.GetExtension(fileName);
            string basename = Path.GetFileNameWithoutExtension(fileName);
            string newFileName = Path.Combine(dir, $"{basename}_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_fff")}{ext}");
            File.Move(fileName, newFileName);
            string[] files = Directory.GetFiles(myLoggingDirectory, $"{Path.GetFileNameWithoutExtension(myFileBaseName)}*{Path.GetExtension(myFileBaseName)}");
            string [] sortedByAgeDescending = files.Select(f => new FileInfo(f)).OrderByDescending(x => x.LastWriteTime).Select(x => x.FullName).ToArray();
            
            for(int i=myMaxGenerationsToKeep;i<sortedByAgeDescending.Length;i++)
            {
                try
                {
                    Delete(sortedByAgeDescending[i]);
                }
                catch(Exception)
                {

                }
            }

            ReopenFileAndWriteHeader();
        }

        internal Action<string> Delete = File.Delete;

        public void Log(string message)
        {
            lock (myLock)
            {
                if (myFile.Position > myMaxFileSizeInBytes)
                {
                    RollOver();
                }

                myLog.WriteLine(message);
                myLog.Flush(); // ensure that even in case of crash we get our data written to disk and out of StreamWriter buffer
            }
        }

        public void Dispose()
        {
            lock (myLock)
            {
                myLog?.Dispose();
                myLog = null;
            }
        }

        public static FileLogger Logger = new FileLogger(null, maxFileSizeInMB:30, maxGenerationsToKeep:4);
    }
}
