using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;
using ServiceWire.ZeroKnowledge;

namespace ServiceWire.Messaging
{
    public class MsgClient : IMsgClient, IDisposable
    {
        private readonly string _identity;
        private readonly string _identityKey;
        private readonly string _connectionString;
        private readonly ILog _logger;
        private readonly IStats _stats;

        private readonly Guid _clientId;
        private readonly byte[] _clientIdBytes;

        private readonly DealerSocket _dealerSocket;
        private readonly NetMQPoller _socketPoller;
        private readonly NetMQPoller _clientPoller;
        private readonly NetMQQueue<List<byte[]>> _sendQueue;
        private readonly NetMQQueue<List<byte[]>> _receiveQueue;
        private readonly NetMQTimer _heartBeatTimer = null;
        private readonly int _heartBeatMs;
        private readonly int _maxSkippedHeartBeatReplies;

        private ZkClientSession _session = null;
        private bool _throwOnSend = false;
        private bool _hostDead = false;

        /// <summary>
        /// Client constructor.
        /// </summary>
        /// <param name="connectionString">Valid NetMQ client socket connection string.</param>
        /// <param name="identity">Client identifier passed to the server in Zero Knowledge authentication. 
        ///                  Null for unsecured hosts.</param>
        /// <param name="identityKey">Secret key used by NOT passed to the server in Zero Knowledge authentication 
        ///                   but used in memory to validate authentication of the server. Null for 
        ///                   unsecured hosts</param>
        /// <param name="logger">ILogger implementation for logging operations. Null is replaced with NullLogger.</param>
        /// <param name="stats">IStats implementation for logging perf metrics. Null is replaced with NullStats.</param>
        /// <param name="heartBeatIntervalMs">Number of milliseconds between client sending heartbeat message to the server. 
        /// Default 30,000 (30 seconds). Min is 1000 (1 second) and max is 600,000 (10 mins).</param>
        /// <param name="maxSkippedHeartBeatReplies">Maximum heartbeat intervals skipped without a heartbeat reply 
        /// from the server before the client begins throwing on Send and returns false for the IsHostAlive property.
        /// Default is 3. Min is 1 and max is 10.</param>
        public MsgClient(string connectionString, string identity = null, string identityKey = null, 
            ILog logger = null, IStats stats = null, 
            int heartBeatIntervalMs = 30000, int maxSkippedHeartBeatReplies = 3)
        {
            _identity = identity;
            _identityKey = identityKey;
            _connectionString = connectionString;
            _logger = logger ?? new NullLogger();
            _stats = stats ?? new NullStats();

            _heartBeatMs = (heartBeatIntervalMs < 1000) 
                ? 1000 
                : (heartBeatIntervalMs > 600000) ? 600000 : heartBeatIntervalMs;

            _maxSkippedHeartBeatReplies = (maxSkippedHeartBeatReplies < 1)
                ? 1
                : (maxSkippedHeartBeatReplies > 10) ? 10 : maxSkippedHeartBeatReplies;

            _clientId = Guid.NewGuid();
            _clientIdBytes = _clientId.ToByteArray();

            _sendQueue = new NetMQQueue<List<byte[]>>();
            _dealerSocket = new DealerSocket();
            _dealerSocket.Options.Identity = _clientIdBytes;
            _dealerSocket.Connect(_connectionString);
            _sendQueue.ReceiveReady += SendQueue_ReceiveReady;
            _dealerSocket.ReceiveReady += DealerSocket_ReceiveReady;
            _socketPoller = new NetMQPoller { _dealerSocket, _sendQueue };
            _socketPoller.RunAsync();

            _receiveQueue = new NetMQQueue<List<byte[]>>();
            _receiveQueue.ReceiveReady += ReceivedQueue_ReceiveReady;

            if (null != _identity && null != _identityKey)
            {
                _throwOnSend = true;
                _heartBeatTimer = new NetMQTimer(_heartBeatMs);
                _heartBeatTimer.Elapsed += HeartBeatTimer_Elapsed;
                _clientPoller = new NetMQPoller { _receiveQueue, _heartBeatTimer };
                _heartBeatTimer.Enable = true;
                _logger.Debug("Client created with protocol enabled.");
            }
            else
            {
                _clientPoller = new NetMQPoller { _receiveQueue };
                _logger.Debug("Client created. Protocol NOT enabled.");
            }
            _clientPoller.RunAsync();
        }

        public ZkClientSession Session { get { return _session; } }

        private void HeartBeatTimer_Elapsed(object sender, NetMQTimerEventArgs e)
        {
            //check for last heartbeat from server, set to throw on new send if exceeds certain threshold
            if (null !=_session && null != _session.Crypto)
            {
                if ((DateTime.UtcNow - _session.LastHeartBeat).TotalMilliseconds 
                    > _heartBeatMs * _maxSkippedHeartBeatReplies)
                {
                    _throwOnSend = true; //do not allow send
                    _hostDead = true;
                    _logger.Debug("Heartbeat from server skipped {0} time. Host is dead.", 
                        _maxSkippedHeartBeatReplies);
                }
                else
                {
                    _logger.Debug("Heartbeat sent.");
                    HeartBeatsSentCount++;
                    _sendQueue.Enqueue(new List<byte[]> { MessageHeader.HeartBeat });
                }
            }
            else
            {
                _throwOnSend = true; //do not allow send
            }
        }

        private ManualResetEvent _securedSignal = null;

        /// <summary>
        /// Executes the Zero Knowledge protocol and blocks until it is complete of has failed.
        /// Allows client to be hooked up to protocol events.
        /// </summary>
        /// <param name="blockUntilComplete">If true (default) method blocks until protocol is established.</param>
        /// <param name="timeoutMs">If blocking, will block for timeoutMs.</param>
        /// <returns>Returns true if connection has been secured. False if non-blocking or if protocol fails.</returns>
        public bool SecureConnection(bool blockUntilComplete = true, int timeoutMs = 500)
        {
            if (null == _identity || null == _identityKey) return false;
            if (null != _session && null != _session.Crypto) return true; //in case it's called twice

            _securedSignal = new ManualResetEvent(false);
            _session = new ZkClientSession(_identity, _identityKey, _logger);
            _sendQueue.Enqueue(_session.CreateInitiationRequest());
            _logger.Debug("Protocol initiation request sent.");

            if (blockUntilComplete)
            {
                if (_securedSignal.WaitOne(timeoutMs))
                {
                    if (!_throwOnSend) return true; //success
                }
            }
            return false;
        }

        public Guid ClientId { get { return _clientId; } }
        public bool IsHostAlive { get { return !_hostDead; } }
        public DateTime? LastHeartBeatReceivedFromHost {
            get {
                return _session?.LastHeartBeat;
            }
        }
        public int HeartBeatsSentCount { get; private set; }
        public int HeartBeatsReceivedCount { get; private set; }

        private EventHandler<MessageEventArgs> _receivedEvent;
        private EventHandler<MessageEventArgs> _receivedHeartbeatEvent;
        private EventHandler<MessageEventArgs> _invalidReceivedEvent;
        private EventHandler<EventArgs> _ecryptionProtocolEstablishedEvent;
        private EventHandler<ProtocolFailureEventArgs> _ecryptionProtocolFailedEvent;

        /// <summary>
        /// This event occurs when a message has been received. 
        /// </summary>
        /// <remarks>This handler is thread safe occuring on a thread other 
        /// than the thread sending and receiving messages over the wire.</remarks>
        public event EventHandler<MessageEventArgs> MessageReceived {
            add {
                _receivedEvent += value;
            }
            remove {
                _receivedEvent -= value;
            }
        }

        /// <summary>
        /// This event occurs when an a heartbeat response message has been received. 
        /// </summary>
        /// <remarks>This handler is thread safe occuring on a thread other 
        /// than the thread sending and receiving messages over the wire.</remarks>
        public event EventHandler<MessageEventArgs> HeartbeatReceived {
            add {
                _receivedHeartbeatEvent += value;
            }
            remove {
                _receivedHeartbeatEvent -= value;
            }
        }

        /// <summary>
        /// This event occurs when an invalid protocol message has been received. 
        /// </summary>
        /// <remarks>This handler is thread safe occuring on a thread other 
        /// than the thread sending and receiving messages over the wire.</remarks>
        public event EventHandler<MessageEventArgs> InvalidMessageReceived {
            add {
                _invalidReceivedEvent += value;
            }
            remove {
                _invalidReceivedEvent -= value;
            }
        }

        /// <summary>
        /// This event occurs when the client has established a secure connection and
        /// messages may be sent without throwing an operation cancelled exception. 
        /// </summary>
        /// <remarks>This handler is thread safe occuring on a thread other 
        /// than the thread sending and receiving messages over the wire.</remarks>
        public event EventHandler<EventArgs> EcryptionProtocolEstablished {
            add {
                _ecryptionProtocolEstablishedEvent += value;
            }
            remove {
                _ecryptionProtocolEstablishedEvent -= value;
            }
        }

        /// <summary>
        /// This event occurs when the client failes to establish a secure connection and
        /// messages may be sent without throwing an operation cancelled exception. 
        /// </summary>
        /// <remarks>This handler is thread safe occuring on a thread other 
        /// than the thread sending and receiving messages over the wire.</remarks>
        public event EventHandler<ProtocolFailureEventArgs> EcryptionProtocolFailed {
            add {
                _ecryptionProtocolFailedEvent += value;
            }
            remove {
                _ecryptionProtocolFailedEvent -= value;
            }
        }

        public bool CanSend { get { return !_throwOnSend; } }

        public void Send(List<byte[]> frames)
        {
            if (_disposed) throw new ObjectDisposedException("Client", "Cannot send on disposed client.");
            if (null == frames || frames.Count == 0)
            {
                _logger.Error("Empty message send attempt failed.");
                throw new ArgumentException("Cannot be null or empty.", nameof(frames));
            }
            if (_throwOnSend)
            {
                _logger.Error("Message send attempt failed. Protocol not established.");
                throw new OperationCanceledException("Encryption protocol not established.");
            }
            _sendQueue.Enqueue(frames);
        }

        public void Send(IEnumerable<byte[]> frames)
        {
            Send(frames.ToList());
        }

        public void Send(byte[] frame)
        {
            Send(new[] { frame });
        }

        public void Send(List<string> frames)
        {
            Send(frames, Encoding.UTF8);
        }

        public void Send(IEnumerable<string> frames)
        {
            Send(frames, Encoding.UTF8);
        }

        public void Send(params string[] frames)
        {
            Send(Encoding.UTF8, frames);
        }

        public void Send(string frame)
        {
            Send(frame, Encoding.UTF8);
        }

        public void Send(List<string> frames, Encoding encoding)
        {
            Send((from n in frames
                  select n == null
                    ? (byte[])null
                    : encoding.GetBytes(n)).ToList());
        }

        public void Send(IEnumerable<string> frames, Encoding encoding)
        {
            Send((from n in frames
                  select n == null
                    ? (byte[])null
                    : encoding.GetBytes(n)).ToList());
        }

        public void Send(Encoding encoding, params string[] frames)
        {
            Send((from n in frames
                  select n == null
                    ? (byte[])null
                    : encoding.GetBytes(n)).ToList());
        }

        public void Send(string frame, Encoding encoding)
        {
            Send(new[] { frame == null
                    ? (byte[])null
                    : encoding.GetBytes(frame) });
        }


        //Executes on same poller thread as dealer socket, so we can send directly
        private void SendQueue_ReceiveReady(object sender, NetMQQueueEventArgs<List<byte[]>> e)
        {
            var message = new NetMQMessage();
            message.AppendEmptyFrame();
            List<byte[]> frames;
            if (e.Queue.TryDequeue(out frames, new TimeSpan(1000)))
            {
                if (null != _session && null != _session.Crypto)
                {
                    //do not encrypt heartbeat message
                    if (frames.Count > 1 || !frames[0].IsEqualTo(MessageHeader.HeartBeat))
                    {
                        //encrypt message frames of regular messages but not heartbeat messages
                        for (int i = 0; i < frames.Count; i++)
                        {
                            frames[i] = _session.Crypto.Encrypt(frames[i]);
                        }
                    }
                }

                foreach (var frame in frames)
                {
                    message.Append(frame);
                }
                _dealerSocket.SendMultipartMessage(message);
                _logger.Debug("Message sent. Frame count: {0}", message.FrameCount);
            }
        }

        //Executes on same poller thread as dealer socket, so we enqueue to the received queue
        //and raise the event on the client poller thread for received queue
        private void DealerSocket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var msg = e.Socket.ReceiveMultipartMessage();
            _logger.Debug("Message received. Frame count: {0}", msg.FrameCount);
            var message = msg.ToMessageWithoutClientFrame(_clientId);
            _receiveQueue.Enqueue(message.Frames);
        }

        //Executes on client poller thread to avoid tying up the dealer socket poller thread
        private void ReceivedQueue_ReceiveReady(object sender, NetMQQueueEventArgs<List<byte[]>> e)
        {
            List<byte[]> frames;
            if (e.Queue.TryDequeue(out frames, new TimeSpan(1000)))
            {
                if (frames[0].IsEqualTo(MessageHeader.HeartBeat))
                {
                    ProcessHeartBeatResponse(frames);
                }
                else if (_throwOnSend && null != _session && null == _session.Crypto)
                {
                    if (IsHandshakeReply(frames))
                    {
                        ProcessProtocolExchange(frames);
                    }
                    else
                    {
                        OnMessageReceivedFailure(frames);
                    }
                }
                else
                {
                    ProcessRegularMessage(frames);
                }
            }
        }

        private void ProcessHeartBeatResponse(List<byte[]> frames)
        {
            HeartBeatsReceivedCount++;
            _session?.RecordHeartBeat();
            _receivedHeartbeatEvent?.Invoke(this, new MessageEventArgs
            {
                Message = new Message
                {
                    ClientId = _clientId,
                    Frames = frames
                }
            });
        }

        private void ProcessRegularMessage(List<byte[]> frames)
        {
            if (null != _session && null != _session.Crypto)
            {
                //decrypt message frames
                for (int i = 0; i < frames.Count; i++)
                {
                    frames[i] = _session.Crypto.Decrypt(frames[i]);
                }
            }
            _logger.Info("Message received Frame count {0}. Total length {1}.", 
                frames.Count, frames.Select(x => x.Length).Sum());
            _receivedEvent?.Invoke(this, new MessageEventArgs
            {
                Message = new Message
                {
                    ClientId = _clientId,
                    Frames = frames
                }
            });
        }

        private void OnMessageReceivedFailure(List<byte[]> frames)
        {
            _logger.Error("Message cannot be processed.");
            _invalidReceivedEvent?.Invoke(this, new MessageEventArgs
            {
                Message = new Message
                {
                    ClientId = _clientId,
                    Frames = frames
                }
            });
        }

        private void ProcessProtocolExchange(List<byte[]> frames)
        {
            string error = null;
            if (frames[0][2] == MessageHeader.SM0)
            {
                //send handshake request
                var frames1 = _session.CreateHandshakeRequest(_identity, frames);
                if (null != frames1)
                {
                    _sendQueue.Enqueue(frames1);
                    _logger.Debug("Protocol handshake request sent.");
                }
                else
                {
                    error = "Protocol handshake creation failed.";
                    _logger.Fatal(error);
                    _ecryptionProtocolFailedEvent?.Invoke(this, new ProtocolFailureEventArgs { Message = error });
                }
            }
            else if (frames[0][2] == MessageHeader.SM1)
            {
                //send proof
                var frames2 = _session.CreateProofRequest(frames);
                if (null != frames2)
                {
                    _sendQueue.Enqueue(frames2);
                    _logger.Debug("Protocol proof request sent.");
                }
                else
                {
                    error = "Protocol proof creation failed."; 
                    _logger.Fatal(error);
                    _ecryptionProtocolFailedEvent?.Invoke(this, new ProtocolFailureEventArgs { Message = error });
                }
            }
            else if (frames[0][2] == MessageHeader.SM2)
                
            {
                if (_session.ProcessProofReply(frames)) //complete proof
                {
                    _throwOnSend = false;
                    if (null != _securedSignal) _securedSignal.Set(); //signal if waiting
                    _logger.Info("Protocol successfully established.");
                    _ecryptionProtocolEstablishedEvent?.Invoke(this, new EventArgs());
                }
                else
                {
                    error = "Protocol process proof failed.";
                    _logger.Fatal(error);
                    _ecryptionProtocolFailedEvent?.Invoke(this, new ProtocolFailureEventArgs { Message = error });
                }
            }
            else
            {
                error = $"Server returned protocol failure code {frames[0][2]}.";
                _logger.Fatal(error);
                _ecryptionProtocolFailedEvent?.Invoke(this, new ProtocolFailureEventArgs { Message = error });
            }
        }

        private bool IsHandshakeReply(List<byte[]> frames)
        {
            return (null != frames
                && (frames.Count == 2 || frames.Count == 3)
                && frames[0].Length == 4
                && frames[0][0] == MessageHeader.SOH
                && frames[0][1] == MessageHeader.ACK
                && (frames[0][2] == MessageHeader.FF0
                   || frames[0][2] == MessageHeader.SM0
                   || frames[0][2] == MessageHeader.SF0
                   || frames[0][2] == MessageHeader.SM1
                   || frames[0][2] == MessageHeader.SF1
                   || frames[0][2] == MessageHeader.SM2
                   || frames[0][2] == MessageHeader.SF2)
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
                    if (null != _heartBeatTimer) _heartBeatTimer.Enable = false;
                    if (null != _socketPoller) _socketPoller.Dispose();
                    if (null != _sendQueue) _sendQueue.Dispose();
                    if (null != _dealerSocket) _dealerSocket.Dispose();

                    if (null != _clientPoller) _clientPoller.Dispose();
                    if (null != _receiveQueue) _receiveQueue.Dispose();
                    if (null != _securedSignal) _securedSignal.Dispose();
                }
            }
        }

        #endregion
    }
}
