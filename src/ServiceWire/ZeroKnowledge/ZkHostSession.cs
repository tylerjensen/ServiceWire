using ServiceWire.Messaging;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace ServiceWire.ZeroKnowledge
{
    public class ZkHostSession : IEquatable<ZkHostSession>
    {
        private readonly ZkProtocol _protocol;
        private readonly IZkRepository _repository;
        private readonly Guid _clientId;
        private readonly DateTime _created;
        private readonly ILog _logger;

        private string _clientIpAddress = null;
        private string _identity = null;
        private ZkPasswordHash _identityHash = null;
        private byte[] _scramble = null;
        private byte[] _serverSessionKey = null;
        private byte[] _clientEphemeralA = null;
        private byte[] _serverEphemeralB = null;

        private ZkCrypto _zkCrypto = null;
        private DateTime _lastHeartbeatReceived = DateTime.UtcNow;
        private DateTime _lastMessageReceived = DateTime.UtcNow;

        private RSAParameters _serverPublicPrivateKey = default;
        private RSAParameters _serverPublicKey = default;
        private RSAParameters _clientPublicKey = default;


        public ZkHostSession(IZkRepository repository, Guid clientId, ILog logger)
        {
            _repository = repository;
            _clientId = clientId;
            _logger = logger ?? new NullLogger();
            _created = DateTime.UtcNow;
            _lastMessageReceived = _created;
            _protocol = new ZkProtocol();
        }

        public Guid ClientId { get { return _clientId; } }
        public string ClientIpAddress { get { return _clientIpAddress; } }
        public string ClientIdentity { get { return _identity; } }
        public DateTime Created { get { return _created; } }
        public DateTime LastMessageReceived { get { return _lastMessageReceived; } }
        public DateTime LastHeartbeatReceived { get { return _lastHeartbeatReceived; } }
        public int HeartBeatsReceivedCount { get; private set; }
        public int MessagesReceivedCount { get; private set; }
        public void RecordHeartBeat()
        {
            HeartBeatsReceivedCount++;
            _lastHeartbeatReceived = DateTime.UtcNow;
        }
        public void RecordMessageReceived()
        {
            MessagesReceivedCount++;
            _lastMessageReceived = DateTime.UtcNow;
        }

        public ZkCrypto Crypto
        {
            get
            {
                return _zkCrypto;
            }
        }

        public List<byte[]> ProcessProtocolRequest(Message message)
        {
            var frames = message.Frames;
            if (frames[0][2] == ZkMessageHeader.CM0)
                return ProcessInitiationRequest(message);
            else if (frames[0][2] == ZkMessageHeader.CM1)
                return ProcessHandshakeRequest(message);
            else
                return ProcessProofRequest(message);
        }

        private List<byte[]> ProcessInitiationRequest(Message message)
        {
            var frames = message.Frames;
            var list = new List<byte[]>();
            if (frames.Count != 2)
            {
                list.Add(ZkMessageHeader.HandshakeResponseFailure);
                list.Add(_protocol.ComputeHash(_protocol.CryptRand()));
                list.Add(_protocol.ComputeHash(_protocol.CryptRand()));
                _logger.Debug("Protocol initiation failed for {0}.", message.ClientId);
            }
            else
            {
                _clientPublicKey = frames[1].ToRSAParameters();
                using (var rsa = new RSACryptoServiceProvider())
                {
                    _serverPublicPrivateKey = rsa.ExportParameters(true);
                    _serverPublicKey = rsa.ExportParameters(false);
                }
                list.Add(ZkMessageHeader.InititaionResponseSuccess);
                list.Add(_serverPublicKey.ToBytes());
                _logger.Debug("Protocol initiation completed for {0}.", message.ClientId);
            }
            return list;
        }

        private List<byte[]> ProcessHandshakeRequest(Message message)
        {
            var frames = message.Frames;
            var list = new List<byte[]>();
            if (frames.Count != 4)
            {
                list.Add(ZkMessageHeader.HandshakeResponseFailure);
                list.Add(_protocol.ComputeHash(_protocol.CryptRand()));
                list.Add(_protocol.ComputeHash(_protocol.CryptRand()));
                _logger.Debug("Protocol handshake failed for {0}.", message.ClientId);
            }
            else
            {
                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.ImportParameters(_serverPublicPrivateKey);
                    _identity = Encoding.UTF8.GetString(rsa.Decrypt(frames[1], RSAEncryptionPadding.Pkcs1));
                    _clientEphemeralA = rsa.Decrypt(frames[2], RSAEncryptionPadding.Pkcs1);
                    _clientIpAddress = Encoding.UTF8.GetString(rsa.Decrypt(frames[3], RSAEncryptionPadding.Pkcs1));
                }
                _identityHash = _repository.GetPasswordHashSet(_identity);

                if (null == _identityHash)
                {
                    list.Add(ZkMessageHeader.HandshakeResponseFailure);
                    list.Add(_protocol.ComputeHash(_protocol.CryptRand()));
                    list.Add(_protocol.ComputeHash(_protocol.CryptRand()));
                    _logger.Debug("Protocol handshake failed for {0}.", message.ClientId);
                }
                else
                {
                    _serverEphemeralB = _protocol.GetServerEphemeralB(_identityHash.Salt,
                    _identityHash.Verifier, _protocol.CryptRand());

                    _scramble = _protocol.CalculateRandomScramble(_clientEphemeralA, _serverEphemeralB);

                    _serverSessionKey = _protocol.ServerComputeSessionKey(_identityHash.Salt, _identityHash.Key,
                        _clientEphemeralA, _serverEphemeralB, _scramble);

                    list.Add(ZkMessageHeader.HandshakeResponseSuccess);
                    using (var rsa = new RSACryptoServiceProvider())
                    {
                        rsa.ImportParameters(_clientPublicKey);
                        list.Add(rsa.Encrypt(_identityHash.Salt, RSAEncryptionPadding.Pkcs1));
                        list.Add(rsa.Encrypt(_serverEphemeralB, RSAEncryptionPadding.Pkcs1));
                    }
                    _logger.Debug("Protocol handshake completed for {0}.", message.ClientId);
                }
            }
            return list;
        }

        private List<byte[]> ProcessProofRequest(Message message)
        {
            var frames = message.Frames;
            if (frames.Count != 2) throw new ArgumentException("Invalid frame count.", nameof(frames));

            byte[] clientSessionHash = frames[1];

            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(_serverPublicPrivateKey);
                clientSessionHash = rsa.Decrypt(frames[1], RSAEncryptionPadding.Pkcs1);
            }

            var serverClientSessionHash = _protocol.ClientCreateSessionHash(_identity, _identityHash.Salt,
                _clientEphemeralA, _serverEphemeralB, _serverSessionKey);

            var list = new List<byte[]>();
            if (!clientSessionHash.IsEqualTo(serverClientSessionHash))
            {
                list.Add(ZkMessageHeader.ProofResponseFailure);
                list.Add(_protocol.ComputeHash(_protocol.CryptRand()));
                _logger.Debug("Protocol proof failed for {0}.", message.ClientId);
            }
            else
            {
                var serverSessionHash = _protocol.ServerCreateSessionHash(_clientEphemeralA,
                    clientSessionHash, _serverSessionKey);
                _zkCrypto = new ZkCrypto(_serverSessionKey, _scramble, _logger);

                list.Add(ZkMessageHeader.ProofResponseSuccess);
                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.ImportParameters(_clientPublicKey);
                    list.Add(rsa.Encrypt(serverSessionHash, RSAEncryptionPadding.Pkcs1));
                }
                _logger.Debug("Protocol proof completed for {0}.", message.ClientId);
            }
            return list;
        }

        bool IEquatable<ZkHostSession>.Equals(ZkHostSession other)
        {
            return Equals(other);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ZkHostSession);
        }

        public bool Equals(ZkHostSession other)
        {
            return _clientId.Equals(other);
        }

        public override int GetHashCode()
        {
            return _clientId.GetHashCode();
        }
    }
}
