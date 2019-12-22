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

[assembly:AssemblyFileVersion("1.0.2.0")]
[assembly: AssemblyVersion("1.0.2.0")]
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
            $"by Alois Kraus 2019 v{Assembly.GetExecutingAssembly().GetName().Version}" + Environment.NewLine +
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

            HostFactory.Run(x =>
                {
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

        private static void Dynamic_All(TraceEvent obj)
        {
            Console.WriteLine(obj);
        }

        private static void Parser_OnWMIOperationStart(WMIStart obj)
        {
            Console.WriteLine("{obj}");
        }

        // According to https://social.technet.microsoft.com/Forums/windowsserver/en-US/445ddb5e-16b4-4dca-ab89-c0d0ec728af5/boot-order-of-windows-services
        // we can influence the service startup order by specifying a service group. Scheduler is started as one of the first services
        // so we can be pretty sure to get everything in place before anything interesting happens
        // We just need to start early enough before WMI Service is up and it can be used. 
        // Service Group Start Order is defined in Computer\HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\ServiceGroupOrder
        private static void RegisterAsEarlyStartedServiceAndAutoLogger()
        {
            ImportAutoLoggerFile();

            string serviceKeyBase = @"SYSTEM\CurrentControlSet\Services";
            string serviceKeyName =  serviceKeyBase + "\\" + ServiceName;
            var key = Registry.LocalMachine.OpenSubKey(serviceKeyName, true);
            if( key == null )
            {
                key = Registry.LocalMachine.OpenSubKey(serviceKeyBase, true);
                key = key.CreateSubKey(ServiceName);
            }

            // start service as early as possible 
            // but we still loose some events since all services are started concurrently after the servicemain was entered
            key.SetValue("Group", "Video", RegistryValueKind.String);
        }

        private static void ImportAutoLoggerFile()
        {
            string dir = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            string fullPathRegFile = Path.Combine(dir, AutoLoggerRegFileName);
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