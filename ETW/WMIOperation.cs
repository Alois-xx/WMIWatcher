namespace WMIWatcher.ETW
{
    public enum WMIOperation
    {
        Other,
        Connect,
        ExecQuery,
        ExecMethod,
        ExecAsync,
        CreateEnumerator,
        Disconnect,
        NotificationQuery,
        ProcessEnd,
    }
}
