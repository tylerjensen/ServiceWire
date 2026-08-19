using System;

namespace ServiceWire
{
    /// <summary>
    /// Capabilities a 7.0+ server advertises to clients through
    /// <see cref="ServiceSyncInfo.CapabilityFlags"/>. A client may only use a
    /// capability after seeing the server advertise it, so servers that predate a
    /// flag never receive traffic they cannot handle.
    /// </summary>
    [Flags]
    public enum ProtocolCapabilities
    {
        None = 0,

        /// <summary>
        /// The server accepts MethodInvocation2 framed requests: length-prefixed
        /// frames with correlation ids and the v2 parameter encodings.
        /// </summary>
        WireV2 = 1,
    }
}
