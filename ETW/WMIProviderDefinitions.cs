namespace WMIWatcher.ETW
{
    //C> logman query providers Microsoft-Windows-WMI-Activity
    //
    // Provider GUID
    // -------------------------------------------------------------------------------
    // Microsoft - Windows - WMI - Activity           { 1418EF04 - B0B4 - 4623 - BF7E - D74AB47BBDAA}
    //
    //                Value Keyword              Description
    // ------------------------------------------------------------------------------
    // 0x8000000000000000  Microsoft - Windows - WMI - Activity / Trace
    // 0x4000000000000000  Microsoft - Windows - WMI - Activity / Operational
    // 0x2000000000000000  Microsoft - Windows - WMI - Activity / Debug
    // xperf command: Microsoft-Windows-WMI-Activity:0xe000000000000000:0x5:'stack'
    public static class WMIProviderDefinitions
    {
        public const ulong Keyword_WMI_Activity_Trace = 0x8000000000000000L;
        public const ulong Keyworkd_WMI_Activity_Operational = 0x4000000000000000L;
        public const ulong Keyword_WMI_Activity_Debug = 0x2000000000000000L;
        public const ulong Keyword_ALL = 0xffffffffffffffffL;

        /// <summary>
        /// ETW WMI Provider Name
        /// </summary>
        public const string WMI_Activity_Provider_Name = "Microsoft-Windows-WMI-Activity";

        public const int WMI_Activity_Start = 11;
        public const int WMI_Activity_ExecAsync = 12;
        public const int WMI_Activity_Disconnect = 13;
        public const int WMI_Activity_Transfer = 50;
    }


}
