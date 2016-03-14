#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


namespace ServiceWire
{
    /// <summary>
    ///     The channel behavior exposed on the client level.
    /// </summary>
    public interface IDvChannel
    {
        #region  Others

        /// <summary>
        ///     Returns true if client is connected to the server.
        /// </summary>
        bool IsConnected { get; }

        #endregion
    }
}