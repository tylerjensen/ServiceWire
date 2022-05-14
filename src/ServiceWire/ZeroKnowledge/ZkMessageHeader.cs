namespace ServiceWire.ZeroKnowledge
{
    internal sealed class ZkMessageHeader
    {
        internal const byte SOH = 0x01; //start of header
        internal const byte ENQ = 0x05; //enquiry -- signal protocol handshake message
        internal const byte ACK = 0x06; //acknowledgment -- signal protocol handshake reply
        internal const byte BEL = 0x07; //bell -- signal regular message 

        internal const byte CM1 = 0x08;
        internal const byte CM2 = 0x09;
        internal const byte SM1 = 0x0A;
        internal const byte SF1 = 0x0B;
        internal const byte SM2 = 0x0C;
        internal const byte SF2 = 0x0D;
        internal const byte FF0 = 0x0E;

        internal const byte CM0 = 0x10; //initiation request
        internal const byte SM0 = 0x11; //initiation response success
        internal const byte SF0 = 0x12; //initiation response failure

        internal static byte[] InitiationRequest = new byte[] { SOH, ENQ, CM0, BEL };
        internal static byte[] HandshakeRequest = new byte[] { SOH, ENQ, CM1, BEL };
        internal static byte[] ProofRequest = new byte[] { SOH, ENQ, CM2, BEL };

        internal static byte[] ProtocolResponseFailure = new byte[] { SOH, ACK, FF0, BEL };

        internal static byte[] InititaionResponseSuccess = new byte[] { SOH, ACK, SM0, BEL };
        internal static byte[] InititaionResponseFailure = new byte[] { SOH, ACK, SF0, BEL };

        internal static byte[] HandshakeResponseSuccess = new byte[] { SOH, ACK, SM1, BEL };
        internal static byte[] HandshakeResponseFailure = new byte[] { SOH, ACK, SF1, BEL };

        internal static byte[] ProofResponseSuccess = new byte[] { SOH, ACK, SM2, BEL };
        internal static byte[] ProofResponseFailure = new byte[] { SOH, ACK, SF2, BEL };

        internal static byte[] HeartBeat = new byte[] { SOH, ACK, BEL, BEL };
    }
}
