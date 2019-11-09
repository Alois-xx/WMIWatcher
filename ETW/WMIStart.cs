using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WMIWatcher.ETW
{
    public class WMIStart : EventBase
    {
        const string Operation_Connect = "IWbemServices::Connect";
        const string Operation_ExecQuery = "Start IWbemServices::ExecQuery";
        const string Operation_EnumerateClass = "Start IWbemServices::CreateClassEnum";
        const string Operation_EnumerateInstance = "Start IWbemServices::CreateInstanceEnum";
        const string Operation_ExecMethod = "Start IWbemServices::ExecMethod";
        // This is where WMI polling queries are registered
        const string Operation_ExecNotificatonQuery = "Start IWbemServices::ExecNotificationQuery";


        public string Query { get; private set; }
        public string NameSpace { get; private set; }
        public int OperationId { get; }
        public int GroupOperationid { get; }
        public WMIOperation Operation { get; }
        public int ClientProcessId { get; }
        public DateTime ClientProcessCreationTime { get; }
        public string ClientProcess { get; set; }
        public bool IsRemoteQuery { get; }

        public WMIStart(TraceEvent @event, TraceLogEventSource realtimeSource, TraceLog log) : base(@event)
        {
            // Parts of the event are
            //[0]	"CorrelationId"	string
            //[1]	"GroupOperationId"	string
            //[2]	"OperationId"	string
            //[3]	"Operation"	string
            //[4]	"ClientMachine"	string
            //[5]	"ClientMachineFQDN"	string
            //[6]	"User"	string
            //[7]	"ClientProcessId"	string
            //[8]	"ClientProcessCreationTime"	string
            //[9]	"NamespaceName"	string
            //[10]	"IsLocal"	string
            // IWbemServices::CreateClassEnum - root\wmi : MSNT_SystemTrace
            // IWbemServices::ExecQuery - root\cimv2 : select Eventcode,Eventtype,Logfile,Message,Sourcename,TimeGenerated,user from Win32_NTLogEvent where eventtype = 1

            // throw if wrong event is tried to parse
            OperationId = (int)@event.ID switch
            {
                WMIProviderDefinitions.WMI_Activity_Start => (int)@event.PayloadByName("OperationId"),
                _ => throw new InvalidOperationException($"Event with ID {WMIProviderDefinitions.WMI_Activity_Start} expected. But got: {@event.ID}")
            };

            ClientProcessCreationTime = DateTime.FromFileTime((Int64)@event.PayloadByName("ClientProcessCreationTime"));

            ClientProcessId = (int)@event.PayloadByName("ClientProcessId");

            TraceProcess process = realtimeSource?.TraceLog.Processes.Where(p => p.ProcessID == ClientProcessId).FirstOrDefault();

            // The process start time by Realtime ETW tracing is only correct when the process was started during that time. Otherwise it gets only the current day as start date
            if (process != null)
            {
                ClientProcess = $"{process.CommandLine}";
            }
            else
            {
                ClientProcess = "Unknown Process - Use ClientProcessId";
            }

            GroupOperationid = (int)@event.PayloadByName("GroupOperationId");
            IsRemoteQuery = !(bool)@event.PayloadByName("IsLocal");

            // operation is a string which can have several values
            string operation = @event.PayloadByName("Operation").ToString();

            if (operation.IndexOf(Operation_ExecQuery) != -1)
            {
                Operation = WMIOperation.ExecQuery;
                ExtractNameSpaceAndQuery(Operation_ExecQuery, operation);
            }
            else if (operation.IndexOf(Operation_EnumerateClass) != -1)
            {
                Operation = WMIOperation.CreateEnumerator;
                ExtractNameSpaceAndQuery(Operation_EnumerateClass, operation);
            }
            else if (operation.IndexOf(Operation_EnumerateInstance) != -1)
            {
                Operation = WMIOperation.CreateEnumerator;
                ExtractNameSpaceAndQuery(Operation_EnumerateInstance, operation);
            }
            else if (operation.IndexOf(Operation_ExecMethod) != -1)
            {
                Operation = WMIOperation.ExecMethod;
                ExtractNameSpaceAndQuery(Operation_ExecMethod, operation);
            }
            else if( operation.IndexOf(Operation_ExecNotificatonQuery) != -1)
            {
                Operation = WMIOperation.NotificationQuery;
                ExtractNameSpaceAndQuery(Operation_ExecNotificatonQuery, operation);
            }
            else if (operation.IndexOf(Operation_Connect) != -1)
            {
                Operation = WMIOperation.Connect;
                Query = "";
            }
            else
            {
                Operation = WMIOperation.Other;
                Query = operation;
            }
        }

        void ExtractNameSpaceAndQuery(string prefixString, string operation)
        {
            string nameSpaceAndQuery = operation?.Substring(prefixString.Length + 3);
            int firstColon = nameSpaceAndQuery.IndexOf(':');
            if (firstColon != -1)
            {
                Query = nameSpaceAndQuery?.Substring(firstColon + 2);
                NameSpace = nameSpaceAndQuery?.Substring(0, firstColon - 1);
            }

        }

        public override string ToString()
        {
            return $"{TimeStamp.ToString("yyyy.MM.dd HH:mm: ss.fff")} {Operation} {Query} NameSpace {NameSpace} IsRemote: {IsRemoteQuery} {ClientProcess} {ClientProcessId}";
        }
    }

}
