namespace ServiceWire
{
    /// <summary>
    /// The channel behavior exposed on the client level.
    /// </summary>
    public interface IDvChannel
    {
        /// <summary>
        /// Returns true if client is connected to the server.
        /// </summary>
        bool IsConnected { get; }
    }
}