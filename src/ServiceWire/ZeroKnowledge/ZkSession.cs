using System;
using System.Diagnostics;
using System.IO;

namespace ServiceWire.ZeroKnowledge
{
    public class ZkSession
    {
        private readonly IZkRepository _repository;
        private readonly ZkProtocol _zkProtocol = new ZkProtocol();
        private ZkPasswordHash _zkPasswordHash = null;
        private ZkCrypto _zkCrypto = null;

        private readonly ILog _logger;
        private readonly IStats _stats;

        private string _username = null;
        private byte[] _aEphemeral = null;
        private byte[] _bEphemeral = null;
        private byte[] _bRand = null;
        private byte[] _scramble = null;
        private byte[] _serverSessionKey = null;
        private byte[] _clientSessionHash = null;
        private byte[] _serverSessionHash = null;

        public ZkSession(IZkRepository respository, ILog logger, IStats stats)
        {
            _repository = respository;
            _logger = logger ?? new NullLogger();
            _stats = stats ?? new NullStats();
        }

        public ZkCrypto Crypto { get { return _zkCrypto; } }

        public bool ProcessZkProof(BinaryReader binReader, BinaryWriter binWriter, Stopwatch sw)
        {
            _clientSessionHash = binReader.ReadBytes(32);
            _logger.Debug("ZkProof client session hash received: {0}", Convert.ToBase64String(_clientSessionHash));
            var serverClientSessionHash = _zkProtocol.ClientCreateSessionHash(_username, _zkPasswordHash.Salt, 
                _aEphemeral, _bEphemeral, _serverSessionKey);

            _serverSessionHash = _zkProtocol.ServerCreateSessionHash(_aEphemeral, _clientSessionHash, _serverSessionKey);
            if (!_clientSessionHash.IsEqualTo(serverClientSessionHash))
            {
                _logger.Debug("ZkProof session hash does not match. Authentication failed. Server session hash: {0}", Convert.ToBase64String(_serverSessionHash));
                binWriter.Write(false);
                return false;
            }
            _zkCrypto = new ZkCrypto(_serverSessionKey, _scramble);
            binWriter.Write(true);
            binWriter.Write(_serverSessionHash);
            _logger.Debug("ZkProof session hash sent to client: {0}", Convert.ToBase64String(_serverSessionHash));
            return true;
        }

        public bool ProcessZkInitiation(BinaryReader binReader, BinaryWriter binWriter, Stopwatch sw)
        {
            _username = binReader.ReadString();
            _aEphemeral = binReader.ReadBytes(32);
            _logger.Debug("ZkInitiation client username received: {0}", _username);
            _logger.Debug("ZkInitiation client Ephemeral received: {0}", Convert.ToBase64String(_aEphemeral));
            _zkPasswordHash = _repository.GetPasswordHashSet(_username);
            if (null == _zkPasswordHash)
            {
                _logger.Debug("ZkInitiation client username not found. Authentication failed.");
                binWriter.Write(false);
                return false;
            }
            _bRand = _zkProtocol.CryptRand();
            _bEphemeral = _zkProtocol.GetServerEphemeralB(_zkPasswordHash.Salt, _zkPasswordHash.Verifier, _bRand);
            _scramble = _zkProtocol.CalculateRandomScramble(_aEphemeral, _bEphemeral);
            _serverSessionKey = _zkProtocol.ServerComputeSessionKey(_zkPasswordHash.Salt, 
                _zkPasswordHash.Key, _aEphemeral, _bEphemeral, _scramble);

            binWriter.Write(true);
            binWriter.Write(_zkPasswordHash.Salt);
            _logger.Debug("ZkInitiation hash salt sent to client: {0}", Convert.ToBase64String(_zkPasswordHash.Salt));
            binWriter.Write(_bEphemeral);
            _logger.Debug("ZkInitiation server Ephemeral sent to client: {0}", Convert.ToBase64String(_bEphemeral));
            return true;
        }
    }
}
