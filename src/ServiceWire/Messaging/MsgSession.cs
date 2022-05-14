using System;
using ServiceWire.ZeroKnowledge;

namespace ServiceWire.Messaging
{
    public class MsgSession
    {
        private readonly ZkHostSession _session;

        public MsgSession(ZkHostSession session)
        {
            _session = session;
        }

        public ZkHostSession Session
        {
            get { return _session; }
        }

        public Guid ClientId {
            get {
                return _session.ClientId;
            }
        }
        public string ClientIpAddress {
            get {
                return _session.ClientIpAddress;
            }
        }
        public string ClientIdentity {
            get {
                return _session.ClientIdentity;
            }
        }

        public DateTime Created {
            get {
                return _session.Created;
            }
        }
        public DateTime LastMessageReceived {
            get {
                return _session.LastMessageReceived;
            }
        }
        public DateTime LastHeartbeatReceived {
            get {
                return _session.LastHeartbeatReceived;
            }
        }
        public int HeartBeatsReceivedCount {
            get {
                return _session.HeartBeatsReceivedCount;
            }
        }
        public int MessagesReceivedCount {
            get {
                return _session.MessagesReceivedCount;
            }
        }
    }
}
