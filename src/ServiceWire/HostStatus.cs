namespace ServiceWire
{
    /// <summary>
    /// Describes the lifecycle state of a ServiceWire host listener.
    /// </summary>
    public enum HostStatus
    {
        Created,
        Opening,
        Open,
        Faulted,
        Closed
    }
}
