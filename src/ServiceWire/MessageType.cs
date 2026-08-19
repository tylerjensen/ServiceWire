namespace ServiceWire
{
    public enum MessageType
    {
        TerminateConnection = 0,
        MethodInvocation = 1,
        ReturnValues = 2,
        UnknownMethod = 3,
        ThrowException = 4,
        SyncInterface = 5,

        /// <summary>Framed v2 request; sent only after the server advertises ProtocolCapabilities.WireV2.</summary>
        MethodInvocation2 = 6,
        /// <summary>Framed v2 response carrying a correlation id and a status byte.</summary>
        Response2 = 7,

        ZkInitiate = 20,
        ZkProof = 21
    };
}
