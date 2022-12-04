using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Topshelf;
using WMIWatcher.ETW;

[assembly:AssemblyFileVersion("1.0.6.0")]
[assembly: AssemblyVersion("1.0.6.0")]
[assembly: InternalsVisibleTo("WMIWatcher_uTest")]

namespace WMIWatcher
{
    class Program
    {
        const string ServiceName = "WMIWatcher";
        const string AutoLoggerRegFileName = "WMIActivity_AutoLogger.reg";
        const string AutoLoggerRegistryRootKey = @"HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\WmiWatcher";

        static readonly string HelpStr =
            "WMIWatcher [install] [uninstall]" + Environment.NewLine +
            $"by Alois Kraus 2019-2022 v{Assembly.GetExecutingAssembly().GetName().Version}" + Environment.NewLine +
            "\tinstall       Install WMIWatcher as system service which is started at boot to capture all WMI queries which are written to WMIWatcher.csv" + Environment.NewLine +
            "\tuninstall     Uninstall system service" + Environment.NewLine +
            "\t==============================================================" + Environment.NewLine + 
            "\tWhen started with no arguments it will start watching for WMI queries and print the output to console and write data to WMIWatcher.csv" + Environment.NewLine +
            "\tThe file WmiWatcher.csv is rolled over at 30 MB and will be kept for the last 4 generations. Older files will be deleted" + Environment.NewLine
            ;

        static void Main(string[] args)
        {
            if( args.Length == 0) // Console Mode
            {
                Help();
            }

            if ( !UACHelper.UACHelper.IsElevated || !UACHelper.UACHelper.IsAdministrator)
            {
                Console.WriteLine("Error: You must be administrator and run with elevated privileges.");
                return;
            }

            HostFactory.Run(x =>
                {
                    x.UnhandledExceptionPolicy = Topshelf.Runtime.UnhandledExceptionPolicyCode.TakeNoAction;
                    x.Service<Service>();
                    x.EnableServiceRecovery(r => r.RestartService(TimeSpan.FromSeconds(10)));
                    x.SetServiceName(ServiceName);
                    x.AfterInstall(ImportAutoLoggerFile);
                    x.AfterUninstall(CleanRegistryFromAutoLogger);
                    x.StartAutomatically();
                }
            );
        }

        private static void Help()
        {
            Console.WriteLine(HelpStr);
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("Cancel pressed");
            Service.RunningService.Dispose();
        }

        private static void ImportAutoLoggerFile()
        {
            string fullPathRegFile = Path.Combine(AppContext.BaseDirectory, AutoLoggerRegFileName);
            string cmdLine = $"IMPORT \"{fullPathRegFile}\"";
            StartReg(cmdLine);
        }


        private static void CleanRegistryFromAutoLogger()
        {
            string cmdLine = $"DELETE {AutoLoggerRegistryRootKey} /f";
            StartReg(cmdLine);
        }

        static void StartReg(string cmdLine)
        {
            var proc = new ProcessStartInfo("reg.exe", cmdLine)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };
            var p = Process.Start(proc);
            Console.WriteLine($"Reg.exe Output from command {cmdLine}");
            Console.WriteLine(p.StandardOutput.Read());
            p.WaitForExit();
        }
    }
}