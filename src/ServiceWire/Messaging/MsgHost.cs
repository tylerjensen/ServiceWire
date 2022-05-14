using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetMQ;
using NetMQ.Sockets;
using ServiceWire.ZeroKnowledge;

namespace ServiceWire.Messaging
{
    public class MsgHost : IMsgHost, IDisposable
    {
        private readonly string _connectionString;
        private readonly ILog _logger;
        private readonly IStats _stats;

        private readonly RouterSocket _routerSocket;
        private readonly NetMQPoller _socketPoller;

        private readonly NetMQPoller _hostPoller;
        private readonly NetMQQueue<NetMQMessage> _sendQueue;
        private readonly NetMQQueue<Message> _receivedQueue;
        private readonly NetMQQueue<MessageFailure> _sendFailureQueue;
        private readonly NetMQTimer _sessionCleanupTimer = null;

        private readonly IZkRepository _authRepository;
        private readonly ConcurrentDictionary<Guid, ZkHostSession> _sessions;
        private readonly int _sessionTimeoutMins;

        /// <summary>
        /// Host constructor. Supply an IZkRepository to enable Zero Knowledge authentication and encryption.
        /// </summary>
        /// <param name="connectionString">Valid NetMQ server socket connection string.</param>
        /// <param name="authRepository">External authentication repository. Null creates host with no encryption.</param>
        /// <param name="logger">ILogger implementation for logging operations. Null is replaced with NullLogger.</param>
        /// <param name="stats">IStats implementation for logging perf metrics. Null is replaced with NullStats.</param>
        /// <param name="sessionTimeoutMins">Session timout check interval. If no heartbeats or messages 
        /// received on a given session in this period of time, the session will be removed from memory 
        /// and futher attempts from the client will fail. Default is 20 minutes. Min is 1 and Max is 3600.</param>
        public MsgHost(string connectionString, IZkRepository authRepository = null, 
            ILog logger = null, IStats stats = null, int sessionTimeoutMins = 20)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException("Connection string cannot be null.", nameof(connectionString));
            _connectionString = connectionString;
            _logger = logger ?? new NullLogger();
            _stats = stats ?? new NullStats();

            //enforce min and max session check
            _sessionTimeoutMins = (sessionTimeoutMins < 1)
                ? 1
                : (sessionTimeoutMins > 3600) ? 3600 : sessionTimeoutMins;

            _authRepository = authRepository;
            _sessions = new ConcurrentDictionary<Guid, ZkHostSession>();
            _sendQueue = new NetMQQueue<NetMQMessage>();

            _routerSocket = new RouterSocket(_connectionString);
            _routerSocket.Options.RouterMandatory = true;
            _sendQueue.ReceiveReady += SendQueue_ReceiveReady;
            _routerSocket.ReceiveReady += RouterSocket_ReceiveReady;
            _socketPoller = new NetMQPoller { _routerSocket, _sendQueue };
            _socketPoller.RunAsync();

            _sendFailureQueue = new NetMQQueue<MessageFailure>();
            _receivedQueue = new NetMQQueue<Message>();
            _sendFailureQueue.ReceiveReady += SendFailureQueue_ReceiveReady;
            _receivedQueue.ReceiveReady += ReceivedQueue_ReceiveReady;

            if (null == _authRepository || _authRepository is ZkNullRepository)
            {
                _hostPoller = new NetMQPoller { _receivedQueue, _sendFailureQueue };
                _logger.Debug("Hoste created. Protocol NOT enabled.");
            }
            else
            {
                _sessionCleanupTimer = new NetMQTimer(new TimeSpan(0, _sessionTimeoutMins, 0));
                _sessionCleanupTimer.Elapsed += SessionCleanupTimer_Elapsed;
                _hostPoller = new NetMQPoller { _receivedQueue, _sendFailureQueue, _sessionCleanupTimer };
                _sessionCleanupTimer.Enable = true;
                _logger.Debug("Host created with protocol enabled.");
            }
            _hostPoller.RunAsync();
        }

        private EventHandler<MessageEventFailureArgs> _sentFailureEvent;
        private EventHandler<MessageEventArgs> _receivedEvent;
        private EventHandler<MessageEventArgs> _receivedHeartbeatEvent;
        private EventHandler<MessageEventArgs> _zkClientSessionEstablishedEvent;

        public Guid[] GetCurrentSessionKeys()
        {
            return _sessions.Keys.ToArray();
        }

        public MsgSession[] GetCurrentSessions()
        {
            var sessions = (from n in _sessions.Values select new MsgSession(n)).ToArray();
            return sessions;
        }

        public void RemoveSession(Guid key)
        {
            ZkHostSession session;
            _sessions.TryRemove(key, out session);
        }

        public MsgSession GetSession(Guid key)
        {
            ZkHostSession session;
            if (_sessions.TryGetValue(key, out session))
            {
                return new MsgSession(session);
            }
            return null;
        }

        /// <summary>
        /// This event occurs when a message has been received. 
        /// </summary>
        /// <remarks>This handler will run on a different thread than the socket poller and
        /// blocking on this thread will not block sending and receiving.</remarks>
        public event EventHandler<MessageEventArgs> MessageReceived {
            add {
                _receivedEvent += value;
            }
            remove {
                _receivedEvent -= value;
            }
        }

        /// <summary>
        /// This event occurs when a heartbeat message has been received from a client.
        /// </summary>
        /// <remarks>This handler will run on a different thread than the socket poller and
        /// blocking on this thread will not block sending and receiving.</remarks>
        public event EventHandler<MessageEventArgs> ReceivedHeartbeatEvent {
            add {
                _receivedHeartbeatEvent += value;
            }
            remove {
                _receivedHeartbeatEvent -= value;
            }
        }

        /// <summary>
        /// This event occurs when a message failed to send because the client is no longer connected.
        /// </summary>
        /// <remarks>This handler will run on a different thread than the socket poller and
        /// blocking on this thread will not block sending and receiving.</remarks>
        public event EventHandler<MessageEventFailureArgs> MessageSentFailure {
            add {
                _sentFailureEvent += value;
            }
            remove {
                _sentFailureEvent -= value;
            }
        }

        /// <summary>
        /// This event occurs when a new client session has been established over Zero Knowledge protocol.
        /// The Message in the event contains no frames. It is only to signal a new encrypted ClientId.
        /// </summary>
        /// <remarks>This handler will run on a different thread than the socket poller and
        /// blocking on this thread will not block sending and receiving.</remarks>
        public event EventHandler<MessageEventArgs> ZkClientSessionEstablishedEvent {
            add {
                _zkClientSessionEstablishedEvent += value;
            }
            remove {
                _zkClientSessionEstablishedEvent -= value;
            }
        }

        public void Send(Message message)
        {
            Send(message.ClientId, message.Frames);
        }

        public void Send(Guid clientId, List<byte[]> frames)
        {
            if (_disposed) throw new ObjectDisposedException("Client", "Cannot send on disposed client.");
            if (null == frames || frames.Count == 0)
            {
                _logger.Error("Send message failed. Empty message.");
                throw new ArgumentException("Cannot be null or empty.", nameof(frames));
            }
            var message = new NetMQMessage();
            message.Append(clientId.ToByteArray());
            message.AppendEmptyFrame();
            if (null != _authRepository && !(_authRepository is ZkNullRepository))
            {
                ZkHostSession session = null;
                if (!_sessions.TryGetValue(clientId, out session)) session = null;
                if (null != session && null != session.Crypto)
                {
                    foreach (var frame in frames) message.Append(session.Crypto.Encrypt(frame));
                }
                else
                {
                    _logger.Error("Send message failed. Protocol not established for this client {0}.", clientId);
                    throw new OperationCanceledException("Encrypted session not established.");
                }
            }
            else
            {
                foreach (var frame in frames) message.Append(frame);
            }
            _sendQueue.Enqueue(message); //send by message to socket poller
        }

        public void Send(Guid clientId, IEnumerable<byte[]> frames)
        {
            Send(clientId, frames.ToList());
        }

        public void Send(Guid clientId, byte[] frame)
        {
            Send(clientId, new[] { frame });
        }

        public void Send(Guid clientId, List<string> frames)
        {
            Send(clientId, frames, Encoding.UTF8);
        }

        public void Send(Guid clientId, IEnumerable<string> frames)
        {
            Send(clientId, frames, Encoding.UTF8);
        }

        public void Send(Guid clientId, params string[] frames)
        {
            Send(clientId, Encoding.UTF8, frames);
        }

        public void Send(Guid clientId, string frame)
        {
            Send(clientId, frame, Encoding.UTF8);
        }

        public void Send(Guid clientId, List<string> frames, Encoding encoding)
        {
            Send(clientId, (from n in frames
                  select n == null
                    ? (byte[])null
                    : encoding.GetBytes(n)).ToList());
        }

        public void Send(Guid clientId, IEnumerable<string> frames, Encoding encoding)
        {
            Send(clientId, (from n in frames
                  select n == null
                    ? (byte[])null
                    : encoding.GetBytes(n)).ToList());
        }

        public void Send(Guid clientId, Encoding encoding, params string[] frames)
        {
            Send(clientId, (from n in frames
                  select n == null
                    ? (byte[])null
                    : encoding.GetBytes(n)).ToList());
        }

        public void Send(Guid clientId, string frame, Encoding encoding)
        {
            Send(clientId, new[] { frame == null
                    ? (byte[])null
                    : encoding.GetBytes(frame) });
        }


        //occurs on socket polling thread to assure sending and receiving on same thread
        private void SendQueue_ReceiveReady(object sender, NetMQQueueEventArgs<NetMQMessage> e)
        {
            NetMQMessage message;
            if (e.Queue.TryDequeue(out message, new TimeSpan(1000)))
            {
                try
                {
                    _routerSocket.SendMultipartMessage(message);
                    _logger.Info("Message sent with {0} frames.", message.FrameCount);
                }
                catch (Exception ex) //clientId not found or other error, raise event
                {
                    var unreachable = ex as HostUnreachableException;
                    _logger.Error("Error sending message to {0} with {1} frames.", new Guid(message.First.Buffer), message.FrameCount);
                    _sendFailureQueue.Enqueue(new MessageFailure
                    {
                        Message = message.ToMessageWithClientFrame(),
                        ErrorCode = null != unreachable ? unreachable.ErrorCode.ToString() : "Unknown",
                        ErrorMessage = ex.Message
                    });
                }
            }
        }

        //occurs on socket polling thread to assure sending and receiving on same thread
        private void RouterSocket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var msg = e.Socket.ReceiveMultipartMessage();
            if (null == msg || msg.FrameCount < 2) return; //ignore this message - nothing in it
            _logger.Debug("Message received. Frame count: {0}", msg.FrameCount);
            var message = msg.ToMessageWithClientFrame();
            _receivedQueue.Enqueue(message); //sends by message to host poller
        }

        //occurs on host polling thread to allow sending and receiving on a different thread
        private void SendFailureQueue_ReceiveReady(object sender, NetMQQueueEventArgs<MessageFailure> e)
        {
            MessageFailure mf;
            if (e.Queue.TryDequeue(out mf, new TimeSpan(1000)))
            {
                _logger.Error("Send failed {0}, {1}", mf.ErrorCode, mf.ErrorMessage);
                _sentFailureEvent?.Invoke(this, new MessageEventFailureArgs
                {
                    Failure = mf
                });
            }
        }

        //occurs on host polling thread to allow sending and receiving on a different thread
        private void ReceivedQueue_ReceiveReady(object sender, NetMQQueueEventArgs<Message> e)
        {
            Message message;
            if (e.Queue.TryDequeue(out message, new TimeSpan(1000)))
            {
                if (message.Frames[0].IsEqualTo(MessageHeader.HeartBeat))
                {
                    ProcessHeartBeat(message);
                }
                else if (null == _authRepository || _authRepository is ZkNullRepository)
                {
                    ProcessRegularMessage(message, null);
                }
                else
                {
                    ProcessProtectedMessage(message);
                }
            }
        }

        private void ProcessHeartBeat(Message message)
        {
            ZkHostSession session = null;
            if (!_sessions.TryGetValue(message.ClientId, out session)) session = null;
            if (null != session)
            {
                session.RecordHeartBeat();
                var heartBeatResponse = new NetMQMessage();
                heartBeatResponse.Append(message.ClientId.ToByteArray());
                heartBeatResponse.AppendEmptyFrame();
                heartBeatResponse.Append(MessageHeader.HeartBeat);
                _sendQueue.Enqueue(heartBeatResponse);
                _logger.Info("Heartbeat received from {0} and response sent.", session.ClientId);
                _receivedHeartbeatEvent?.Invoke(this, new MessageEventArgs
                {
                    Message = message
                });
            }
        }

        private void ProcessRegularMessage(Message message, ZkHostSession session)
        {
            _logger.Info("Message received from {0}. Frame count {1}. Total length {2}.",
                message.ClientId, message.Frames.Count, message.Frames.Select(x => x.Length).Sum());
            if (null != session && null != session.Crypto)
            {
                session.RecordMessageReceived();
                for (int i = 0; i < message.Frames.Count; i++)
                {
                    message.Frames[i] = session.Crypto.Decrypt(message.Frames[i]);
                }
            }
            _receivedEvent?.Invoke(this, new MessageEventArgs
            {
                Message = message
            });
        }

        private void ProcessProtectedMessage(Message message)
        {
            ZkHostSession session = null;
            if (!_sessions.TryGetValue(message.ClientId, out session)) session = null;
            if (IsHandshakeRequest(message.Frames))
            {
                ProcessProtocolExchange(message, session);
            }
            else
            {
                ProcessRegularMessage(message, session);
            }
        }

        private void ProcessProtocolExchange(Message message, ZkHostSession session)
        {
            if (null == session)
            {
                session = new ZkHostSession(_authRepository, message.ClientId, _logger);
                _sessions.TryAdd(message.ClientId, session);
                _logger.Debug("Protocol session created for {0}.", message.ClientId);
            }
            var responseFrames = session.ProcessProtocolRequest(message);
            var msg = new NetMQMessage();
            msg.Append(message.ClientId.ToByteArray());
            msg.AppendEmptyFrame();
            foreach (var frame in responseFrames) msg.Append(frame);
            _sendQueue.Enqueue(msg); //send by message to socket poller

            //if second reply and success, raise event, new client session?
            if (responseFrames[0].IsEqualTo(MessageHeader.ProofResponseSuccess))
            {
                _zkClientSessionEstablishedEvent?.Invoke(this, new MessageEventArgs
                {
                    Message = new Message
                    {
                        ClientId = message.ClientId
                    }
                });
            }
        }

        private void SessionCleanupTimer_Elapsed(object sender, NetMQTimerEventArgs e)
        {
            //remove timed out sessions
            var now = DateTime.UtcNow;
            var timedOutKeys = (from n in _sessions
                            where (now - n.Value.LastMessageReceived).TotalMinutes > _sessionTimeoutMins
                            select n.Key).ToArray();
            if (timedOutKeys.Length > 0)
            {
                foreach (var key in timedOutKeys)
                {
                    ZkHostSession session;
                    _sessions.TryRemove(key, out session);
                }
                _logger.Debug("Protocol sessions cleaned up: Count {0}.", timedOutKeys.Length);
            }
        }

        private bool IsHandshakeRequest(List<byte[]> frames)
        {
            return (null != frames
                && (frames.Count == 2 || frames.Count == 4)
                && frames[0].Length == 4
                && frames[0][0] == MessageHeader.SOH
                && frames[0][1] == MessageHeader.ENQ
                && ((frames[0][2] == MessageHeader.CM0 && frames.Count == 2)
                    || (frames[0][2] == MessageHeader.CM1 && frames.Count == 4)
                    || (frames[0][2] == MessageHeader.CM2 && frames.Count == 2))
                && frames[0][3] == MessageHeader.BEL);
        }


        #region IDisposable Members

        private bool _disposed = false;

        public void Dispose()
        {
            //MS recommended dispose pattern - prevents GC from disposing again
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true; //prevent second call to Dispose
                if (disposing)
                {
                    if (null != _sessionCleanupTimer) _sessionCleanupTimer.Enable = false;
                    if (null != _socketPoller) _socketPoller.Dispose();
                    if (null != _sendQueue) _sendQueue.Dispose();
                    if (null != _routerSocket) _routerSocket.Dispose();

                    if (null != _hostPoller) _hostPoller.Dispose();
                    if (null != _receivedQueue) _receivedQueue.Dispose();
                    if (null != _sendFailureQueue) _sendFailureQueue.Dispose();
                }
            }
        }

        #endregion
    }
}
