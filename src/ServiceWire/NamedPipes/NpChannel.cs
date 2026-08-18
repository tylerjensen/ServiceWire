using System;
using System.IO;
using System.IO.Pipes;

namespace ServiceWire.NamedPipes
{
    public class NpChannel : StreamingChannel
    {
        private readonly NamedPipeClientStream _clientStream;
        private readonly NpChannelIdentifier _channelIdentifier;

        /// <summary>
        /// Creates a connection to the concrete object handling method calls on the pipeName server side
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="npEndPoint"></param>
        /// <param name="serializer">Inject your own serializer for complex objects and avoid using the Newtonsoft JSON DefaultSerializer.</param>
        public NpChannel(Type serviceType, NpEndPoint npEndPoint, ISerializer serializer, ICompressor compressor, ILog logger = null, IStats stats = null)
            : base(serializer, compressor, logger, stats)
        {
            _serviceType = serviceType;
            _channelIdentifier = new NpChannelIdentifier(npEndPoint);
            //PipeOptions.Asynchronous: a synchronous pipe handle serializes concurrent
            //ReadFile/WriteFile, which would deadlock the v2 reader thread against
            //writers; an overlapped handle lets a blocked read coexist with writes
            _clientStream = new NamedPipeClientStream(npEndPoint.ServerName, npEndPoint.PipeName,
                PipeDirection.InOut, PipeOptions.Asynchronous);
            _clientStream.Connect(npEndPoint.ConnectTimeOutMs);
            _stream = _clientStream;
            //independent read and write buffers: a shared BufferedStream cannot serve
            //concurrent readers and writers (single buffer, mode switch needs a seek)
            _binReader = new BinaryReader(new BufferedStream(_clientStream));
            _binWriter = new BinaryWriter(new BufferedStream(_clientStream));
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

        protected override IChannelIdentifier ChannelIdentifier => _channelIdentifier;

        public override bool IsConnected { get { return (null != _clientStream) && _clientStream.IsConnected; } }
    }
}
