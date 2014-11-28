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

        private string _username = null;
        private byte[] _aEphemeral = null;
        private byte[] _bEphemeral = null;
        private byte[] _bRand = null;
        private byte[] _scramble = null;
        private byte[] _serverSessionKey = null;
        private byte[] _clientSessionHash = null;
        private byte[] _serverSessionHash = null;

        public ZkSession(IZkRepository respository)
        {
            _repository = respository;
        }

        public ZkCrypto Crypto { get { return _zkCrypto; } }

        public bool ProcessZkProof(BinaryReader binReader, BinaryWriter binWriter, Stopwatch sw)
        {
            _clientSessionHash = binReader.ReadBytes(32);
            var serverClientSessionHash = _zkProtocol.ClientCreateSessionHash(_username, _zkPasswordHash.Salt, 
                _aEphemeral, _bEphemeral, _serverSessionKey);
            if (!_clientSessionHash.IsEqualTo(serverClientSessionHash))
            {
                binWriter.Write(false);
                return false;
            }
            _serverSessionHash = _zkProtocol.ServerCreateSessionHash(_aEphemeral, _clientSessionHash, _serverSessionKey);
            _zkCrypto = new ZkCrypto(_serverSessionKey, _scramble);
            binWriter.Write(true);
            binWriter.Write(_serverSessionHash);
            return true;
        }

        public bool ProcessZkInitiation(BinaryReader binReader, BinaryWriter binWriter, Stopwatch sw)
        {
            _username = binReader.ReadString();
            _aEphemeral = binReader.ReadBytes(32);
            _zkPasswordHash = _repository.GetPasswordHashSet(_username);
            if (null == _zkPasswordHash)
            {
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
            binWriter.Write(_bEphemeral);
            return true;
        }

    }
}
