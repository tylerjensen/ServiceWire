#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


#region Usings

using System.Diagnostics;
using System.IO;

#endregion


namespace ServiceWire.ZeroKnowledge
{
    public class ZkSession
    {
        #region Constractor

        public ZkSession(IZkRepository respository)
        {
            _repository=respository;
        }

        #endregion


        #region Fields

        private readonly IZkRepository _repository;
        private readonly ZkProtocol _zkProtocol=new ZkProtocol();
        private ZkPasswordHash _zkPasswordHash;

        private string _username;
        private byte[] _aEphemeral;
        private byte[] _bEphemeral;
        private byte[] _bRand;
        private byte[] _scramble;
        private byte[] _serverSessionKey;
        private byte[] _clientSessionHash;
        private byte[] _serverSessionHash;

        #endregion


        #region  Proporties

        public ZkCrypto Crypto { get; private set; }

        #endregion


        #region Methods


        #region Public Methods

        public bool ProcessZkProof(BinaryReader binReader,BinaryWriter binWriter,Stopwatch sw)
        {
            _clientSessionHash=binReader.ReadBytes(32);
            var serverClientSessionHash=_zkProtocol.ClientCreateSessionHash(_username,_zkPasswordHash.Salt,_aEphemeral,_bEphemeral,_serverSessionKey);
            if(!_clientSessionHash.IsEqualTo(serverClientSessionHash))
            {
                binWriter.Write(false);
                return false;
            }
            _serverSessionHash=_zkProtocol.ServerCreateSessionHash(_aEphemeral,_clientSessionHash,_serverSessionKey);
            Crypto=new ZkCrypto(_serverSessionKey,_scramble);
            binWriter.Write(true);
            binWriter.Write(_serverSessionHash);
            return true;
        }

        public bool ProcessZkInitiation(BinaryReader binReader,BinaryWriter binWriter,Stopwatch sw)
        {
            _username=binReader.ReadString();
            _aEphemeral=binReader.ReadBytes(32);
            _zkPasswordHash=_repository.GetPasswordHashSet(_username);
            if(null==_zkPasswordHash)
            {
                binWriter.Write(false);
                return false;
            }
            _bRand=_zkProtocol.CryptRand();
            _bEphemeral=_zkProtocol.GetServerEphemeralB(_zkPasswordHash.Salt,_zkPasswordHash.Verifier,_bRand);
            _scramble=_zkProtocol.CalculateRandomScramble(_aEphemeral,_bEphemeral);
            _serverSessionKey=_zkProtocol.ServerComputeSessionKey(_zkPasswordHash.Salt,_zkPasswordHash.Key,_aEphemeral,_bEphemeral,_scramble);

            binWriter.Write(true);
            binWriter.Write(_zkPasswordHash.Salt);
            binWriter.Write(_bEphemeral);
            return true;
        }

        #endregion


        #endregion
    }
}