using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceWire.Messaging
{
    public interface IMsgHost : IDisposable
    {
        event EventHandler<MessageEventArgs> MessageReceived;
        event EventHandler<MessageEventArgs> ReceivedHeartbeatEvent;
        event EventHandler<MessageEventFailureArgs> MessageSentFailure;
        event EventHandler<MessageEventArgs> ZkClientSessionEstablishedEvent;

        Guid[] GetCurrentSessionKeys();
        MsgSession[] GetCurrentSessions();
        MsgSession GetSession(Guid key);
        void RemoveSession(Guid key);
        void Send(Message message);
        void Send(Guid clientId, List<byte[]> frames);
        void Send(Guid clientId, IEnumerable<byte[]> frames);
        void Send(Guid clientId, byte[] frame);

        void Send(Guid clientId, List<string> frames);
        void Send(Guid clientId, IEnumerable<string> frames);
        void Send(Guid clientId, params string[] frames);
        void Send(Guid clientId, string frame);

        void Send(Guid clientId, List<string> frames, Encoding encoding);
        void Send(Guid clientId, IEnumerable<string> frames, Encoding encoding);
        void Send(Guid clientId, Encoding encoding, params string[] frames);
        void Send(Guid clientId, string frame, Encoding encoding);
    }
}