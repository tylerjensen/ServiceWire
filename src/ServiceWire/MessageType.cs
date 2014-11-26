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

        CryptoConnRequest = 20,
        CryptoConnChallenge = 21,
        CryptoConnResponse = 22,
        CryptoConnConfirm = 23,
        CryptoConnError = 24
    };
}
