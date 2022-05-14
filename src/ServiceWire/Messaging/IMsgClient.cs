using ServiceWire.ZeroKnowledge;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceWire.Messaging
{
    public interface IMsgClient : IDisposable
    {
        bool CanSend { get; }
        Guid ClientId { get; }
        ZkClientSession Session { get; }
        int HeartBeatsReceivedCount { get; }
        int HeartBeatsSentCount { get; }
        bool IsHostAlive { get; }
        DateTime? LastHeartBeatReceivedFromHost { get; }

        event EventHandler<EventArgs> EcryptionProtocolEstablished;
        event EventHandler<ProtocolFailureEventArgs> EcryptionProtocolFailed;
        event EventHandler<MessageEventArgs> InvalidMessageReceived;
        event EventHandler<MessageEventArgs> MessageReceived;
        event EventHandler<MessageEventArgs> HeartbeatReceived;

        bool SecureConnection(bool blockUntilComplete = true, int timeoutMs = 500);
        void Send(List<byte[]> frames);
        void Send(IEnumerable<byte[]> frames);
        void Send(byte[] frame);

        void Send(List<string> frames);
        void Send(IEnumerable<string> frames);
        void Send(params string[] frames);
        void Send(string frame);

        void Send(List<string> frames, Encoding encoding);
        void Send(IEnumerable<string> frames, Encoding encoding);
        void Send(Encoding encoding, params string[] frames);
        void Send(string frame, Encoding encoding);        
    }
}