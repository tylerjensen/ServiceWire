using System;
using System.IO;
using System.IO.Pipes;

namespace ServiceWire.NamedPipes
{
    public class NpChannel : StreamingChannel
    {
        private NamedPipeClientStream _clientStream;

        /// <summary>
        /// Creates a connection to the concrete object handling method calls on the pipeName server side
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="npEndPoint"></param>
        /// <param name="serializer">Inject your own serializer for complex objects and avoid using the Newtonsoft JSON DefaultSerializer.</param>
        public NpChannel(Type serviceType, NpEndPoint npEndPoint, ISerializer serializer)
        {
            _serviceType = serviceType;
            _clientStream = new NamedPipeClientStream(npEndPoint.ServerName, npEndPoint.PipeName, PipeDirection.InOut);
            _clientStream.Connect(npEndPoint.ConnectTimeOutMs);
            _stream = new BufferedStream(_clientStream);
            _binReader = new BinaryReader(_clientStream);
            _binWriter = new BinaryWriter(_clientStream);
            _serializer = serializer ?? new DefaultSerializer();
            try
            {
                SyncInterface(_serviceType);
            }
            catch (Exception)
            {
                this.Dispose(true);
                throw;
            }
        }

        public override bool IsConnected { get { return (null != _clientStream) && _clientStream.IsConnected; } }
    }
}
