#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


#region Usings

using System;
using System.IO;
using System.IO.Pipes;

#endregion


namespace ServiceWire.NamedPipes
{
    public class NpChannel:StreamingChannel
    {
        #region Constractor

        /// <summary>
        ///     Creates a connection to the concrete object handling method calls on the pipeName server side
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="npEndPoint"></param>
        public NpChannel(Type serviceType,NpEndPoint npEndPoint)
        {
            _serviceType=serviceType;
            _clientStream=new NamedPipeClientStream(npEndPoint.ServerName,npEndPoint.PipeName,PipeDirection.InOut);
            _clientStream.Connect(npEndPoint.ConnectTimeOutMs);
            _stream=new BufferedStream(_clientStream);
            _binReader=new BinaryReader(_clientStream);
            _binWriter=new BinaryWriter(_clientStream);
            try
            {
                SyncInterface(_serviceType);
            }
            catch(Exception)
            {
                Dispose(true);
                throw;
            }
        }

        #endregion


        #region Fields

        private readonly NamedPipeClientStream _clientStream;

        #endregion


        #region  Others

        public override bool IsConnected
        {
            get { return (null!=_clientStream)&&_clientStream.IsConnected; }
        }

        #endregion
    }
}